using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messageapi
{
    public class ReceivedMessage
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idMessage { get; set; }

        public DateTime ReceivedDate { get; set; }

        public string Description { get; set; }
    }
}
