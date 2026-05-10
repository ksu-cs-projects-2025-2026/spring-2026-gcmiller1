using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentView
{
    public class ContactService
    {
        private readonly string filePath;

        public ContactService()
        {
            filePath = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                "Contacts.json"
            );
        }

        /// <summary>
        /// Loads contacts from json
        /// </summary>
        /// <returns></returns>
        public List<Contact> LoadContacts()
        {
            if (!File.Exists(filePath))
            {
                return new List<Contact>();
            }

            var json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<Contact>();
            }

            return JsonSerializer.Deserialize<List<Contact>>(json) ?? new List<Contact>();
        }
        
        /// <summary>
        /// Saves contacts to json
        /// </summary>
        /// <param name="contacts"></param>
        public void SaveContacts(List<Contact> contacts)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(contacts, options));
        }

        /// <summary>
        /// Gets or creates a contact when on phone call
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public Contact GetCreateContactByPhone(string phoneNumber)
        {
            var contacts = LoadContacts();

            var contact = contacts.FirstOrDefault(c => c.PhoneNumber == phoneNumber);

            if (contact == null)
            {
                contact = new Contact
                {
                    PhoneNumber = phoneNumber,
                    FirstName = "",
                    LastName = "",
                    Email = "",
                    CreatedOn = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")
                };

                contacts.Add(contact);
                SaveContacts(contacts);
            }

            return contact;
        }

        /// <summary>
        /// Updates contact when changes are made
        /// </summary>
        /// <param name="updatedContact"></param>
        public void UpdateContact(Contact updatedContact)
        {
            var contacts = LoadContacts();

            var existing = contacts.FirstOrDefault(c => c.PhoneNumber == updatedContact.PhoneNumber);

            if (existing == null)
            {
                updatedContact.CreatedOn = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");
                contacts.Add(updatedContact);
            }
            else
            {
                existing.FirstName = updatedContact.FirstName;
                existing.LastName = updatedContact.LastName;
                existing.Email = updatedContact.Email;
            }

            SaveContacts(contacts);
        }
    }
}
