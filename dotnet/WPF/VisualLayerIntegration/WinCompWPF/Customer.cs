using System;

namespace WinCompWPF
{
    //Base customer object with a selection of data.
    public class Customer
    {
        public Customer(string id, string firstname, string lastname, DateTime customerSince, bool newslettersubscriber, float[] data)
        {
            this.FirstName = firstname;
            this.LastName = lastname;
            this.ID = id;
            this.CustomerSince = customerSince;
            this.NewsletterSubscriber = newslettersubscriber;
            this.Data = data;
        }

        public float[] Data { get; }
        public string ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CustomerSince { get; set; }
        public bool NewsletterSubscriber { get; set; }
    }
}
