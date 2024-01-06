namespace ObseumEU.Mluvii.Client.Models
{
    public class User
    {
        public int Id { get; set; }
        public string username { get; set; }
        public string firstname { get; set; }
        public string lastName { get; set; }

        public string email { get; set; }

        //This is set only from mluvii
        public bool? enabled { get; set; }

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if (obj == null || !GetType().Equals(obj.GetType())) return false;

            var p = (User)obj;
            return firstname == p.firstname && username == p.username && lastName == p.lastName && email == p.email;
        }
    }
}