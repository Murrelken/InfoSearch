using System.Collections.Generic;
using System.Linq;

namespace InfoSearch
{
    public class TypeForBooleanSearchOperations
    {
        public List<int> Pages { get; set; }

        public TypeForBooleanSearchOperations(List<int> pages, bool isTrue)
        {
            Pages = isTrue
                ? pages
                : Enumerable.Range(1, 100).Where(x => !pages.Contains(x)).ToList();
        }

        private TypeForBooleanSearchOperations(List<int> pages)
        {
            Pages = pages;
        }

        public static TypeForBooleanSearchOperations BooleamnAmd(TypeForBooleanSearchOperations first,
            TypeForBooleanSearchOperations second)
        {
            var newPages = first.Pages.Intersect(second.Pages).ToList();
            return new TypeForBooleanSearchOperations(newPages);
        }

        public static TypeForBooleanSearchOperations BooleanOr(TypeForBooleanSearchOperations first,
            TypeForBooleanSearchOperations second)
        {
            var newPages = first.Pages.Concat(second.Pages)
                .GroupBy(x => x)
                .Select(x => x.First())
                .ToList();
            return new TypeForBooleanSearchOperations(newPages);
        }
    }
}