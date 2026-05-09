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
                $"Email: {Contact.Email}";

            MessageBox.Show(
                info,
                $"{Contact.FirstName} {Contact.LastName}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void RefreshContactDisplay()
        {
            lb_ContactName.Text = $"{Contact.LastName}, {Contact.FirstName}";
        }
    }
}
