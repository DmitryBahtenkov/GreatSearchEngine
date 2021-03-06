using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;

namespace Core.Storage.Tree
{
    public class Tree<K, V> : IIndex<K, V>
    {
        private readonly ITreeNodeManager<K, V> _nodeManager;
        private readonly bool _allowDuplicateKeys;

        public List<K> Keys => _nodeManager.RootNode.Entries.Select(x => x.Item1).ToList();
        public List<Tuple<K, V>> Entries => _nodeManager.RootNode.Entries.ToList();
        public List<V> Values => _nodeManager.RootNode.Entries.Select(x => x.Item2).ToList();
        
        public Tree(ITreeNodeManager<K, V> nodeManager, bool allowDuplicateKeys = false)
        {
	        _nodeManager = nodeManager ?? throw new ArgumentNullException (nameof(nodeManager));
            _allowDuplicateKeys = allowDuplicateKeys;
        }
        
        public async Task<bool> Delete(K key, V value, IComparer<V> valueComparer = null)
        {
            if (false == _allowDuplicateKeys) 
            {
                throw new InvalidOperationException ("This method should be called only from non-unique tree");
            }

            valueComparer ??= Comparer<V>.Default;
			
            var deleted = false;
            var shouldContinue = true;

            try 
            {
                while (shouldContinue)
                {
                    // Iterating to find all entries we wish to delete
                    using (var enumerator = (TreeEnumerator<K, V>) (await LargerThanOrEqualTo(key)).GetEnumerator())
                    {
                        while (true)
                        {
                            // Stop enumerating as soon as we reached the end of the enumerator
                            if (false == enumerator.MoveNext()) {
                                shouldContinue = false;
                                break;
                            }

                            // Current entry
                            var entry = enumerator.Current;

                            // Stop searching as soon as we reach the bound,
                            // where the larger key presents.
                            if (entry.Item1.Equals(key) && valueComparer.Compare(value, entry.Item2) == 0) 
                            {
	                            enumerator.CurrentNode.Remove(enumerator.CurrentEntry);
	                            deleted = true;
	                            break; // Get new e
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
	            // ignored
            }
            
            _nodeManager.SaveChanges();
            return deleted;
        }
        
        public async Task<bool> Delete(K key)
        {
            if (_allowDuplicateKeys) 
            {
                throw new InvalidOperationException ("This method should be called only from unique tree");
            }

            // Find the node tobe deleted using an enumerator
            using(var enumerator = (TreeEnumerator<K, V>) (await LargerThanOrEqualTo(key)).GetEnumerator())
            {
                // If the first element of enumerator is the key we wishes to delete,
                // then tell the enumerator's current node to delete it.
                // Otherwise, consider the key client specified is not found.
                if (enumerator.MoveNext() && _nodeManager.KeyComparer.Compare (enumerator.Current.Item1, key) == 0)
                {
                    enumerator.CurrentNode.Remove (enumerator.CurrentEntry);
                    return true;
                }
            }

            // Return false by default
            return false;
        }
        
        public Task Insert(K key, V value)
        {
            // First find the node where key should be inserted
            var insertionIndex = 0;
            var leafNode = FindNodeForInsertion(key, ref insertionIndex);

            // Duplication check
            if (insertionIndex >= 0 && false == _allowDuplicateKeys) 
            {
                throw new TreeKeyExistsException(key);
            }

            // Now insert to the leaf
            leafNode.InsertAsLeaf(key, value, insertionIndex >= 0 ? insertionIndex : ~insertionIndex);

            // If the leaf is overflow, then split it
            if (leafNode.IsOverflow) 
            {
                TreeNode<K, V> left, right;
                leafNode.Split(out left, out right);
            }

            // Save changes, if any
            _nodeManager.SaveChanges();
            return Task.CompletedTask;
        }
        
        public Task<Tuple<K, V>> Get(K key)
        {
            var insertionIndex = 0;
            var node = FindNodeForInsertion(key, ref insertionIndex);
            if (insertionIndex < 0) 
            {
                return Task.FromResult<Tuple<K, V>>(null);
            }
            return Task.FromResult(node.GetEntry(insertionIndex));
        }
        
        public Task<IEnumerable<Tuple<K, V>>> LargerThan(K key)
        {
	        var startIterationIndex = 0;
	        var node = FindNodeForIteration(key, _nodeManager.RootNode, false, ref startIterationIndex);

	        return Task.FromResult<IEnumerable<Tuple<K, V>>>(new TreeTraverser<K, V>(_nodeManager, node, startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex-1, TreeTraverseDirection.Ascending));
        }
        
        public Task<IEnumerable<Tuple<K, V>>> LargerThanOrEqualTo(K key)
        {
            var startIterationIndex = 0;
            var node = FindNodeForIteration(key, _nodeManager.RootNode, true, ref startIterationIndex);

            return Task.FromResult<IEnumerable<Tuple<K, V>>>(new TreeTraverser<K, V>(_nodeManager, node, (startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex) -1, TreeTraverseDirection.Ascending));
        }
        
        public Task<IEnumerable<Tuple<K, V>>> LessThanOrEqualTo(K key)
        {
            var startIterationIndex = 0;
            var node = FindNodeForIteration(key, _nodeManager.RootNode, false, ref startIterationIndex);

            return Task.FromResult<IEnumerable<Tuple<K, V>>>(new TreeTraverser<K, V>(_nodeManager, node, startIterationIndex >= 0 ? startIterationIndex+1 : ~startIterationIndex, TreeTraverseDirection.Descending));
        }
        
        public Task<IEnumerable<Tuple<K, V>>> LessThan(K key)
        {
            var startIterationIndex = 0;
            var node = FindNodeForIteration(key, _nodeManager.RootNode, true, ref startIterationIndex);

            return Task.FromResult<IEnumerable<Tuple<K, V>>>(new TreeTraverser<K, V>(_nodeManager, node, startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex, TreeTraverseDirection.Descending));
        }

        private static TreeNode<K, V> FindNodeForIteration(K key, TreeNode<K, V> node, bool moveLeft, ref int startIterationIndex)
		{
			// If this node is empty then return it straight away,
			// because it is a non-full root node.
			// Note that we return a bitwise complement of 0, not 0,
			// otherwise caller thinks we found this key at index #0
			if (node.IsEmpty) 
			{
				startIterationIndex = ~0;
				return node;
			}

			// Perform binary search on specified node
			var binarySearchResult = node.BinarySearchEntriesForKey (key, moveLeft);

			// If found, drill down to children node 
			if (binarySearchResult >= 0)
			{
				if (node.IsLeaf) 
				{
					// We reached the leaf node, cant drill down any more.
					// Let's start iterating from there
					startIterationIndex = binarySearchResult;
					return node;
				}

				// What direction to drill down depends on `direction` parameterr
				return FindNodeForIteration(key, node.GetChildNode(moveLeft ? binarySearchResult : binarySearchResult + 1), moveLeft, ref startIterationIndex);
			}
			// Node found, continue searching on the child node which
			// is positiioned at binarySearchResult

			if (false == node.IsLeaf)
			{
				return FindNodeForIteration(key, node.GetChildNode(~binarySearchResult), moveLeft, ref startIterationIndex);
			}
			// Otherwise, this is a leaf node, no more children to search,
			// return this one

			startIterationIndex = binarySearchResult;
			return node;
		}

		/// <summary>
		/// Search for the node that contains given key, starting from given node
		/// </summary>
		private TreeNode<K, V> FindNodeForInsertion (K key, TreeNode<K, V> node, ref int insertionIndex)
		{
			// If this node is empty then return it straight away,
			// because it is a non-full root node.
			// Note that we return a bitwise complement of 0, not 0,
			// otherwise caller thinks we found this key at index #0
			if (node.IsEmpty) 
			{
				insertionIndex = ~0;
				return node;
			}

			// If X=Vi, for some i, then we are done (X has been found).
			var binarySearchResult = node.BinarySearchEntriesForKey(key);
			if (binarySearchResult >= 0)
			{
				if (_allowDuplicateKeys && false == node.IsLeaf) 
				{
					return FindNodeForInsertion(key, node.GetChildNode(binarySearchResult), ref insertionIndex);
				}

				insertionIndex = binarySearchResult;
				return node;
			}
			// Otherwise, continue searching on the child node which
			// is positiioned at binarySearchResult

			if (false == node.IsLeaf)
			{
				return FindNodeForInsertion(key, node.GetChildNode(~binarySearchResult), ref insertionIndex);
			}
			// Otherwise, this is a leaf node, no more children to search,
			// return this one

			insertionIndex = binarySearchResult;
			return node;
		}

		/// <summary>
		/// SEarch for the node that contains given key, starting from the root node
		/// </summary>
		TreeNode<K, V> FindNodeForInsertion(K key, ref int insertionIndex)
		{
			return FindNodeForInsertion(key, _nodeManager.RootNode, ref insertionIndex);
		}
    }
}