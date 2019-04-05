using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdventureLog.Models
{
    public class SearchResultsViewModel
    {
        public Adventure Adventure { get; set; }

        public IEnumerable<Item> SearchResults { get; set; }

        public SearchResultsViewModel()
        {
            Adventure = null;
            SearchResults = new List<Item>();
        }

        public SearchResultsViewModel(Adventure adventure, IEnumerable<Item> searchResults)
        {
            Adventure = adventure;

            if (searchResults != null)
            {
                SearchResults = searchResults;
            }
            else
            {
                SearchResults = new List<Item>();
            }
        }

    }
}