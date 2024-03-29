﻿using DBracket.Net.TCP.DataSync.Example.Utilities;

namespace DBracket.Net.TCP.DataSync.Example.Models
{
    
    internal class Person : PropertyChangedBase
    {
        #region "----------------------------- Private Fields ------------------------------"

        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public Person()
        {
            
        }

        public Person(string name, string lastName, int age, string address)
        {
            Name = name;
            LastName = lastName;
            Age = age;
            Address = address;
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"

        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        [SyncProperty]
        public string Name { get => _name; set { _name = value; OnMySelfChanged(value); } }
        private string _name;
        public string LastName { get => _lastName; set { _lastName = value; OnMySelfChanged(value); } }
        private string _lastName = "Test";
        [SyncProperty]
        public int Age { get => _age; set { _age = value; OnMySelfChanged(value.ToString()); } }
        private int _age;
        public string Address { get => _address; set { _address = value; OnMySelfChanged(value); } }
        private string _address;
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}