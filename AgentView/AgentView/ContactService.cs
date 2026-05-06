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
        
        public void SaveContacts(List<Contact> contacts)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(contacts, options));
        }

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
                    Email = ""
                };

                contacts.Add(contact);
                SaveContacts(contacts);
            }

            return contact;
        }

        public void UpdateContact(Contact updatedContact)
        {
            var contacts = LoadContacts();

            var existing = contacts.FirstOrDefault(c => c.PhoneNumber == updatedContact.PhoneNumber);

            if (existing == null)
            {
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
