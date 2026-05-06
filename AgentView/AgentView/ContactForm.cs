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
    public partial class ContactForm : Form
    {
        public Contact Contact { get; private set; }
        public ContactForm(Contact contact)
        {
            InitializeComponent();
            Contact = contact;
            tb_FirstName.Text = contact.FirstName;
            tb_LastName.Text = contact.LastName;
            tb_Email.Text = contact.Email;
        }

        private void btn_SaveContact_Click(object sender, EventArgs e)
        {
            Contact.FirstName = tb_FirstName.Text.Trim();
            Contact.LastName = tb_LastName.Text.Trim();
            Contact.Email = tb_Email.Text.Trim();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
