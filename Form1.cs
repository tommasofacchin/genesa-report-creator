using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Newtonsoft.Json;
using System.Linq;
using System.Diagnostics;

namespace GenesaReportProgram
{
    public partial class Form1: Form
    {
        List<Info> listInfo;
        List<OrderDetails> listOrderDetails; 
        List<ClientJSON> clients = new List<ClientJSON>();
        List<ItemJSON> items = new List<ItemJSON>();

        string filePathClients = "clients.json";
        string filePathItems = "items.json";
        int rowCounter = 1;
        float totalPrice = 5;

        public class Info
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        public class OrderDetails
        {
            public int row { get; set; }
            public string Item { get; set; }
            public int Qty { get; set; }
            public float Price { get; set; }
        }

        public class ClientJSON
        {
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Address { get; set; }
            public string Loc { get; set; }
            public string Region { get; set; }
            public string CAP { get; set; }
            public string Country { get; set; }
            public string Tel { get; set; }
            public string Mail { get; set; }
        }
        public class ItemJSON
        {
            public string Name { get; set; }
            public float Price { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(filePathClients))
            {
                string json = File.ReadAllText(filePathClients);
                clients = JsonConvert.DeserializeObject<List<ClientJSON>>(json) ?? new List<ClientJSON>();
            }

            if (File.Exists(filePathItems))
            {
                string json = File.ReadAllText(filePathItems);
                items = JsonConvert.DeserializeObject<List<ItemJSON>>(json) ?? new List<ItemJSON>();

                AutoCompleteStringCollection autoCompleteItems = new AutoCompleteStringCollection();
                autoCompleteItems.AddRange(items.Select(i => i.Name).ToArray());
                txtItem.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtItem.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtItem.AutoCompleteCustomSource = autoCompleteItems;

            }

            listInfo = new List<Info>
            {
                new Info { Label = "Data ordine", Value = DateTime.Now.ToString("dd/MM/yyyy")},
                new Info { Label = "Vettore", Value = "Bartolini"},
                new Info { Label = "Telefono", Value = "+39 3392820828"}
            };
            listOrderDetails = new List<OrderDetails>{};

            reportViewer1.LocalReport.ReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "R_ORDER_CONFIRMATION.rdlc");
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("dtInfo", listInfo));
            reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("OrderDetails", listOrderDetails));

            reportViewer1.RefreshReport();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            List<ReportParameter> parameters = new List<ReportParameter>
            {
                new ReportParameter("Nome", txtSurname.Text + " " + txtName.Text),
                new ReportParameter("Indirizzo", txtAddress.Text),
                new ReportParameter("Loc", txtLoc.Text),
                new ReportParameter("Regione", txtCAP.Text + " " + txtRegion.Text),
                new ReportParameter("Stato", txtCountry.Text),
                new ReportParameter("Telefono", txtTel.Text),
                new ReportParameter("TotalPrice", totalPrice.ToString("F2"))
            };

            reportViewer1.LocalReport.SetParameters(parameters);
            reportViewer1.RefreshReport();


            bool exists = clients.Any(c => c.Name.Equals(txtName.Text, StringComparison.OrdinalIgnoreCase) &&
                                  c.Surname.Equals(txtSurname.Text, StringComparison.OrdinalIgnoreCase));

            if (!exists) SaveClient();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (txtItem.Text == "" || txtPrice.Text == "" || txtQty.Text == "")
            {
                return;
            }


            float price;
            float.TryParse(txtPrice.Text, out price);

            int qty;
            int.TryParse(txtQty.Text, out qty);

            totalPrice += price * qty;

            listOrderDetails.Add(new OrderDetails { row = rowCounter++, Item = txtItem.Text, Price = price * qty, Qty = qty});
            reportViewer1.LocalReport.DataSources[1].Value = listOrderDetails;
            reportViewer1.LocalReport.SetParameters(new ReportParameter("TotalPrice", totalPrice.ToString("F2")));
            reportViewer1.RefreshReport();

            bool exists = items.Any(c => c.Name.Equals(txtItem.Text, StringComparison.OrdinalIgnoreCase));
            if (!exists) SaveItems();

            txtItem.Text = "";
            txtPrice.Text = "";
            txtQty.Text = "";
            txtItem.Focus();


        }

        private void txtQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            CheckNumeric(sender, e);
        }

        private void txtPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            CheckNumeric(sender, e);
        }

        private void txtCAP_KeyPress(object sender, KeyPressEventArgs e)
        {
            CheckNumeric(sender, e);
        }

        private void CheckNumeric(object sender, KeyPressEventArgs e)
        {
            System.Windows.Forms.TextBox textBox = sender as System.Windows.Forms.TextBox;

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.' && e.KeyChar != '-')
            {
                e.Handled = true; 
                return;
            }

            if ((e.KeyChar == ',' || e.KeyChar == '.') && textBox.Text.Contains(","))
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar == '-' && textBox.SelectionStart != 0)
            {
                e.Handled = true;
            }
        }

        private void btnSendMail_Click(object sender, EventArgs e)
        {
            if (txtMail.Text == "" || !txtMail.Text.Contains("@")) return;
            SendEmail();
        }

        private void SendEmail()
        {
            try
            {
                byte[] pdf = ExportToPDF();

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("elisaperozzo1976@gmail.com");
                mail.To.Add(txtMail.Text); 
                mail.Subject = "Conferma Ordine";
                mail.Body = "Ciao " + txtName.Text + ", " +
                    "\nti invio la conferma d'ordine in modo che tu possa controllare e nel caso qualcosa non coincida con quello che abbiamo deciso fammi sapere." +
                    "\nUn abbraccio," +
                    "\nElisa";

                MemoryStream pdfStream = new MemoryStream(pdf);
                mail.Attachments.Add(new Attachment(pdfStream, "Report_Ordine.pdf", "application/pdf"));

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("elisaperozzo1976@gmail.com", Environment.GetEnvironmentVariable("GMAIL_APP_PASSWORD", EnvironmentVariableTarget.User));
                smtp.EnableSsl = true;
                


                smtp.Send(mail);

                MessageBox.Show("Email inviata con successo!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore nell'invio dell'email: " + ex.Message);
            }
        }

        private byte[] ExportToPDF()
        {
            Warning[] warnings;
            string[] streamIds;
            string mimeType, encoding, extension;

            string deviceInfo = @"<DeviceInfo>
            <OutputFormat>PDF</OutputFormat>
            <StartPage>1</StartPage>
            <EndPage>1</EndPage>
        </DeviceInfo>";

            byte[] bytes = reportViewer1.LocalReport.Render(
                "PDF", deviceInfo, out mimeType, out encoding, out extension,
                out streamIds, out warnings);

            return bytes;
        }

        private void btnEmpty_Click(object sender, EventArgs e)
        {
            rowCounter = 1;
            totalPrice = 5;
            listOrderDetails.Clear();
            reportViewer1.LocalReport.DataSources[1].Value = listOrderDetails;
            reportViewer1.LocalReport.SetParameters(new ReportParameter("TotalPrice", totalPrice.ToString("F2")));
            reportViewer1.RefreshReport();
        }

        private void SaveClient()
        {
            ClientJSON newClient = new ClientJSON
            {
                Name = txtName.Text.Trim(),
                Surname = txtSurname.Text.Trim(),
                Address = txtAddress.Text.Trim(),
                Loc = txtLoc.Text.Trim(),
                Region = txtRegion.Text.Trim(),
                CAP = txtCAP.Text.Trim(),
                Country = txtCountry.Text.Trim(),
                Tel = txtTel.Text.Trim(),
                Mail = txtMail.Text.Trim()
            };

            bool exists = clients.Any(c =>
                c.Name.Equals(newClient.Name, StringComparison.OrdinalIgnoreCase) &&
                c.Surname.Equals(newClient.Surname, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                clients.Add(newClient); 

                string json = JsonConvert.SerializeObject(clients, Formatting.Indented);
                File.WriteAllText(filePathClients, json);
            }
        }
        private void SaveItems()
        {
            ItemJSON newItem = new ItemJSON
            {
                Name = txtItem.Text.Trim(),
                Price = float.TryParse(txtPrice.Text, out float price) ? price : 0f
            };

            bool exists = items.Any(c => c.Name.Equals(newItem.Name, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                items.Add(newItem);
                string json = JsonConvert.SerializeObject(items, Formatting.Indented);
                File.WriteAllText(filePathItems, json);
            }
        }

        private void txtSurname_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var client = clients.FirstOrDefault(c =>
                        c.Name.Equals(txtName.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        c.Surname.Equals(txtSurname.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (client != null)
            {
                txtAddress.Text = client.Address;
                txtLoc.Text = client.Loc;
                txtRegion.Text = client.Region;
                txtCAP.Text = client.CAP;
                txtCountry.Text = client.Country;
                txtTel.Text = client.Tel;
                txtMail.Text = client.Mail;
            }
        }

        private void txtItem_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var item = items.FirstOrDefault(c => c.Name.Equals(txtItem.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                txtPrice.Text = item.Price.ToString("F2");
            }
        }

        private void btnSaveReport_Click(object sender, EventArgs e)
        {
            try
            {
                string reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");

                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                string fileName = txtName.Text + txtSurname.Text + "_" + DateTime.Now.ToString("dd-MM-yyyy") + ".pdf";
                string filePath = Path.Combine(reportsFolder, fileName);

                byte[] pdfBytes = ExportToPDF();

                File.WriteAllBytes(filePath, pdfBytes);

                MessageBox.Show($"Report salvato con successo in:\n{filePath}", "Salvataggio completato", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il salvataggio: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
