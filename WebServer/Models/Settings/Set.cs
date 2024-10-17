using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebServer.Models.Settings
{
	public class Set
	{



		public Set(string name, string value)
		{
			Name = name;
			Value = value;
			LastUpdate = DateTime.Now;
		}
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Key]
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime LastUpdate { get; set; }



    }
}

