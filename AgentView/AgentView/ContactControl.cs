using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgentView
{
    public partial class ContactControl : UserControl
    {
        public Contact Contact { get; }

        public event EventHandler ContactUpdated;
        public event EventHandler ContactDeleted;
        public event EventHandler<Contact> CallRequested;

        public ContactControl(Contact contact)
        {
            InitializeComponent();
            Contact = contact;
            RefreshContactDisplay();
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete {Contact.FirstName} {Contact.LastName}?",
                "Delete Contact",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                ContactDeleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btn_Edit_Click(object sender, EventArgs e)
        {
            using var form = new ContactForm(Contact);

            if (form.ShowDialog() == DialogResult.OK)
            {
                RefreshContactDisplay();
                ContactUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btn_Info_Click(object sender, EventArgs e)
        {
            var info =
                $"First Name: {Contact.FirstName}{Environment.NewLine}" +
                $"Last Name: {Contact.LastName}{Environment.NewLine}" +
                $"Phone Number: {Contact.PhoneNumber}{Environment.NewLine}" +
                $"Email: {Contact.Email}{Environment.NewLine}" +
                $"Contact Created: {Contact.CreatedOn}";

            MessageBox.Show(
                info,
                $"{Contact.FirstName} {Contact.LastName}",
                MessageBoxButtons.OK,
                MessageBoxIcon.None
            );
        }

        private void RefreshContactDisplay()
        {
            lb_ContactName.Text = $"{Contact.LastName}, {Contact.FirstName}";
        }

        private void btn_Call_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Contact.PhoneNumber))
            {
                MessageBox.Show(
                    "This contact does not have a phone number.",
                    "Cannot Call",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            var result = MessageBox.Show(
                $"Call {Contact.FirstName} {Contact.LastName} at {Contact.PhoneNumber}?",
                "Confirm Outbound Call",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                CallRequested?.Invoke(this, Contact);
            }
        }
    }
}
