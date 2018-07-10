using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTL
{
    public class AuthorPair
    {
        public int ID { set; get; }
        public int SourceAuthorID { get; set; }
        public int TargetAuthorID { get; set; }

        public AuthorPair(int ID, int sourceAuthorID, int targetAuthorID)
        {
            this.ID = ID;
            this.SourceAuthorID = sourceAuthorID;
            this.TargetAuthorID = targetAuthorID;
        }
    }
}
