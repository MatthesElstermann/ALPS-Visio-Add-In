#nullable enable
using System;
using System.Collections.Generic;
namespace PassBpmnConverter.Conversion;

public static class LinqExtensions
{
    public static IEnumerable<TResult> WhereNotNull<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        foreach (var item in source)
        {
            var result = selector(item);
            if (result != null)
            {
                yield return result;
            }
        }
    }
}
