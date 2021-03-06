﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Models;

namespace Core.Helper
{
    public static class CollectionsHelper
    {
        public static IEnumerable<T> Intersect<T>(IEqualityComparer<T> comparer = null, params IEnumerable<T>[] sources)
        {
            var result = sources.FirstOrDefault();
            if (result is null)
            {
                return Array.Empty<T>();
            }
            foreach (var source in sources)
            {
                result = comparer is null 
                    ? result.Intersect(source)
                    : result.Intersect(source, comparer);
            }

            return result;
        }
        
        public static IEnumerable<T> Union<T>(IEqualityComparer<T> comparer = null, params IEnumerable<T>[] sources)
        {
            var result = sources.FirstOrDefault();
            if (result is null)
            {
                return Array.Empty<T>();
            }
            foreach (var source in sources)
            {
                result = comparer is null 
                    ? result.Union(source)
                    : result.Union(source, comparer);
            }

            return result;
        }
    }
}