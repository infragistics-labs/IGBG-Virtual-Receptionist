using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IGBGVirtualReceptionist
{
    public class Person
    {
        private string myName = "N/A";
        private int myAge = 0;

        // Declare a Name property of type string:
        public string Name
        {
            get
            {
                return myName;
            }
            set
            {
                myName = value;
            }
        }

        // Declare an Age property of type int:
        public int Age
        {
            get
            {
                return myAge;
            }
            set
            {
                myAge = value;
            }
        }
        public override string ToString()
        {
            return "Name = " + Name + ", Age = " + Age;
        }
    }
}