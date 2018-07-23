using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace ShowpadSync
{
    public partial class ShowpadSync : Form
    {
        HttpClient client;
        string uploadTarget;
        string myUploadsId;
        public ShowpadSync()
        {
            InitializeComponent();
        }

        private void btnFolderSelect_Click(object sender, EventArgs e)
        {
           if( spFolderBrowser.ShowDialog() == DialogResult.OK)
            {
                spFileWatcher.Path = spFolderBrowser.SelectedPath;
                txtFolder.Text = spFileWatcher.Path;
            }
        }

        private void spFileWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            // File is created - upload to Showpad My Files

            if (e.Name.Contains("jpg"))
            {
                client = new HttpClient();
                client.BaseAddress = new Uri("https://" + txtOrganization.Text + ".showpad.biz");
                var byteArray = new UTF8Encoding().GetBytes(txtClientID.Text + ":" + txtSecret.Text);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                Dictionary<string, string> tokenDetails = null;

                var login = new Dictionary<string, string>
            {
           {"grant_type", "password"},
           {"username", txtUser.Text},
           {"password", txtPassword.Text},
            };
                var response = client.PostAsync("/api/v3/oauth2/token", new FormUrlEncodedContent(login)).Result;
                AddToLog(response.ToString());

                if (response.IsSuccessStatusCode)
                {
                    tokenDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content.ReadAsStringAsync().Result);
                    AddToLog(response.Content.ReadAsStringAsync().Result);
                    if (tokenDetails != null && tokenDetails.Any())
                    {
                        var tokenNo = tokenDetails.FirstOrDefault().Value;
                        client.DefaultRequestHeaders.Remove("Authorization");
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenNo);


                        // upload file

                        MultipartFormDataContent form = new MultipartFormDataContent();

                            // force Showpad to Autolink to root folder
                          form.Add(new StringContent("{\"materialisedPath\":\"/\"}"), "postProcessingInstructions");



                        FileStream fs = File.OpenRead(e.FullPath);
                        var streamContent = new StreamContent(fs);

                        var imageContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

                        form.Add(imageContent, "file", Path.GetFileName(e.FullPath));
                        

                       // response = client.PostAsync("/api/v3/divisions/"+uploadTarget+"/assets.json", form).Result;
                        response = client.PostAsync("/api/v3/divisions/mine/assets.json", form).Result;

                        

                        dynamic resp = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                          Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                        AddToLog(response.Content.ReadAsStringAsync().Result);
                      //  string userid = resp.response.items[0].id;

                      

                    }
                }

            }
        }

       

        void AddToLog(string t)
        {
            txtLog.Text = txtLog.Text + Environment.NewLine + t;
        }
        
        //unused - test code
        private void CreateClient()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("https://" + txtOrganization.Text + ".showpad.biz");
            var byteArray = new UTF8Encoding().GetBytes(txtClientID.Text +":"+txtSecret.Text);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            Dictionary<string, string> tokenDetails = null;
          
            var login = new Dictionary<string, string>
            {
           {"grant_type", "password"},
           {"username", txtUser.Text},
           {"password", txtPassword.Text},
            };
            var response = client.PostAsync("/api/v3/oauth2/token", new FormUrlEncodedContent(login)).Result;
          // AddToLog( response.ToString());

            if (response.IsSuccessStatusCode)
            {
                tokenDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content.ReadAsStringAsync().Result);
              //  AddToLog(response.Content.ReadAsStringAsync().Result);
                if (tokenDetails != null && tokenDetails.Any())
                {
                    var tokenNo = tokenDetails.FirstOrDefault().Value;
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenNo);


                    // get userid  
                    response = client.GetAsync("/api/v3/users.json?email="+HttpUtility.UrlEncode(txtUser.Text)).Result;
                     dynamic resp = JsonConvert.DeserializeObject<dynamic>( response.Content.ReadAsStringAsync().Result);
                  //  Console.WriteLine(userid.response.items[0]);
                    string userid= resp.response.items[0].id;


                    // get users myuploads id 
                    response = client.GetAsync("/api/v3/users/"+ userid + ".json?fields=myUploadsCollectionId").Result;
                     resp = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                //  Console.WriteLine(userid.response.items[0]);
                     myUploadsId = resp.response.myUploadsCollectionId;

                    // get my uploads target 

                    response = client.GetAsync("/api/v3/users/" + userid + "/channels.json?name=My%20Channel&personalContentIncluded=true").Result;
                    resp = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
               //       Console.WriteLine(resp.response.items[0].id);
                     uploadTarget = resp.response.items[0].id;


                }
            }

        }
        

        private void button1_Click(object sender, EventArgs e)
        {
            CreateClient();
        }

        private void ShowpadSync_Load(object sender, EventArgs e)
        {
            txtOrganization.Text = Properties.Settings.Default.Organization;
            txtUser.Text = Properties.Settings.Default.Username;
            txtPassword.Text = Properties.Settings.Default.Password;
            txtClientID.Text = Properties.Settings.Default.ClientID;
            txtSecret.Text = Properties.Settings.Default.Secret;
            txtFolder.Text = Properties.Settings.Default.Folder;
            chkAutoStart.Checked = Properties.Settings.Default.AutoStart;
        }

        private void ShowpadSync_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Organization = txtOrganization.Text;
            Properties.Settings.Default.Username = txtUser.Text;
            Properties.Settings.Default.Password = txtPassword.Text;
            Properties.Settings.Default.ClientID = txtClientID.Text;
            Properties.Settings.Default.Secret = txtSecret.Text;
            Properties.Settings.Default.Folder = txtFolder.Text;
            Properties.Settings.Default.AutoStart = chkAutoStart.Checked;
            Properties.Settings.Default.Save();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
           // CreateClient();
            spFileWatcher.Path = txtFolder.Text;
            spFileWatcher.EnableRaisingEvents = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            spFileWatcher.EnableRaisingEvents = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }
    }
}
