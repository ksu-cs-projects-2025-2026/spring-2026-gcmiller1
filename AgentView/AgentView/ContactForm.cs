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

        public ContactForm(Contact contact, bool lockPhoneNumber = false)
        {
            InitializeComponent();
            Contact = contact;
            tb_FirstName.Text = contact.FirstName;
            tb_LastName.Text = contact.LastName;
            tb_Email.Text = contact.Email;
            SetupPhoneTextBoxes();
            LoadPhoneNumber(contact.PhoneNumber);

            if (lockPhoneNumber)
            {
                LockPhoneNumberFields();
            }
        }

        private void LockPhoneNumberFields()
        {
            tb_PhoneAreaCode.Enabled = false;
            tb_ThreeDigits.Enabled = false;
            tb_FourDigits.Enabled = false;
        }

        private void SetupPhoneTextBoxes()
        {
            tb_PhoneAreaCode.MaxLength = 3;
            tb_ThreeDigits.MaxLength = 3;
            tb_FourDigits.MaxLength = 4;

            tb_PhoneAreaCode.KeyPress += PhoneTextBox_KeyPress;
            tb_ThreeDigits.KeyPress += PhoneTextBox_KeyPress;
            tb_FourDigits.KeyPress += PhoneTextBox_KeyPress;

            tb_PhoneAreaCode.TextChanged += PhoneTextBox_TextChanged;
            tb_ThreeDigits.TextChanged += PhoneTextBox_TextChanged;
            tb_FourDigits.TextChanged += PhoneTextBox_TextChanged;
        }

        private void PhoneTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void PhoneTextBox_TextChanged(object sender, EventArgs e)
        {
            if (sender == tb_PhoneAreaCode && tb_PhoneAreaCode.Text.Length == 3)
            {
                tb_ThreeDigits.Focus();
                tb_ThreeDigits.SelectAll();
            }
            else if (sender == tb_ThreeDigits && tb_ThreeDigits.Text.Length == 3)
            {
                tb_FourDigits.Focus();
                tb_FourDigits.SelectAll();
            }
        }

        private bool IsPhoneNumberValid()
        {
            return tb_PhoneAreaCode.Text.Length == 3 &&
                   tb_ThreeDigits.Text.Length == 3 &&
                   tb_FourDigits.Text.Length == 4;
        }

        private string GetFormattedPhoneNumber()
        {
            return $"+1{tb_PhoneAreaCode.Text}{tb_ThreeDigits.Text}{tb_FourDigits.Text}";
        }

        private void LoadPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return;
            }

            var digitsOnly = "";

            foreach (char c in phoneNumber)
            {
                if (char.IsDigit(c))
                {
                    digitsOnly += c;
                }
            }

            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                digitsOnly = digitsOnly.Substring(1);
            }

            if (digitsOnly.Length == 10)
            {
                tb_PhoneAreaCode.Text = digitsOnly.Substring(0, 3);
                tb_ThreeDigits.Text = digitsOnly.Substring(3, 3);
                tb_FourDigits.Text = digitsOnly.Substring(6, 4);
            }
        }

        private void btn_SaveContact_Click(object sender, EventArgs e)
        {
            if (!IsPhoneNumberValid())
            {
                MessageBox.Show(
                    "Please enter a valid phone number.",
                    "Invalid Phone Number",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            Contact.FirstName = tb_FirstName.Text.Trim();
            Contact.LastName = tb_LastName.Text.Trim();
            Contact.Email = tb_Email.Text.Trim();
            Contact.PhoneNumber = GetFormattedPhoneNumber();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
