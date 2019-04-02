using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdventureLog.Models
{
    public class AdventureHomeModel
    {
        public ICollection<Adventure> Adventures;
        public IDictionary<string, string> Messages;

        public AdventureHomeModel()
        {
            Adventures = new List<Adventure>();
            Messages = new Dictionary<string, string>();
        }

    }
}