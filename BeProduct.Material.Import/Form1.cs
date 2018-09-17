using CsvHelper;
using Newtonsoft.Json;
using OAuth2.Helpers;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeProduct.Material.Import
{

    public class PostModel
    {
        public class Field
        {
            public string id { get; set; }
            public object value { get; set; }
        }
        public Field[] fields { get; set; }

        public class MaterialSizeAndColors
        {
            public IList<ExpandoObject> Colorways { get; set; }
            public class SizeValue
            {
                public string Name { get; set; }
                public bool SampleSize { get; set; }
                public bool Visible { get; set; }
                public string Comments { get; set; }
                public string UOM { get; set; }
                public decimal? Price { get; set; }
                public string Currency { get; set; }
            }
            public List<SizeValue> Sizes { get; set; }
        }


        public class Supplier
        {
            public string Address { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string State { get; set; }
            public string Website { get; set; }
            public string Fax { get; set; }
            public string Zip { get; set; }
            public string SupplierType { get; set; }

        }

        public MaterialSizeAndColors SizeAndColor { get; set; }
        public List<Supplier> Suppliers { get; set; }
    }
    public partial class Form1 : Form
    {
        static readonly string authUrl = "https://id.winks.io/ids";
        static readonly string clientId = "<YOUR_CLIENTID>";
        static readonly string clientSecret = "<YOUR_CLIENT_PASSWORD>";
        static readonly string companyName = "<YOUR_COMPANY_DOMAIN>";

        static string localappdata = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%");
        static readonly string callbackUrl = "http://localhost:8888/";
        static string accessToken = null;
        static string ApiServer = "https://developers.beproduct.com";
        static Dictionary<string, string> folders = new Dictionary<string, string>();
   
        private Dictionary<string, dynamic> folderFields = new Dictionary<string, dynamic>();
        public bool loggedIn = false;

        public Form1()
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(Properties.Settings.Default["BeproductRefreshToken"]?.ToString()))
            {
                loggedIn = true;
            }
            GetStatusConnection(loggedIn);

            progressBar1.Maximum = 100;
            progressBar1.Step = 1;
        }

        private void btLogin_Click(object sender, EventArgs e)
        {
            if (!loggedIn)
            {
                var refreshToken = Auth.GetRefreshToken(authUrl, clientId, clientSecret, callbackUrl);
                if (string.IsNullOrEmpty(refreshToken.RefreshToken))
                {
                    MessageBox.Show("Application encountered an error while communicating with the authentication server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }

                Properties.Settings.Default["BeproductRefreshToken"] = refreshToken.RefreshToken;
                Properties.Settings.Default.Save(); // Saves settings in application configuration file

                accessToken = refreshToken.AccessToken;

                loggedIn = true;

                this.WindowState = FormWindowState.Minimized;
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                Properties.Settings.Default["BeproductRefreshToken"] = string.Empty;
                Properties.Settings.Default.Save(); // Saves settings in application configuration file

                loggedIn = false;
            }

            GetStatusConnection(loggedIn);
            EnableControls(loggedIn);
        }
        private void GetStatusConnection(bool connected)
        {
            if (connected)
            {
                lbLogin.ForeColor = Color.Green;
                lbLogin.BackColor = Color.White;
                lbLogin.Text = "You are connected";
                btLogin.Text = "LOGOUT";
            }
            else
            {
                lbLogin.ForeColor = Color.Red;
                lbLogin.BackColor = Color.White;
                lbLogin.Text = "You are disconnected";
                btLogin.Text = "LOGIN";
            }
        }

        private void EnableControls(bool enable)
        {

        }

        private void btImport_Click(object sender, EventArgs e)
        {
            var filedialog = new OpenFileDialog();

            if (filedialog.ShowDialog() == DialogResult.OK)
            {
                using (var tr = File.OpenText(filedialog.FileName))
                {
                    var records = new List<Dictionary<string, string>>();
                    var csv = new CsvReader(tr);
                    csv.Read();
                    csv.ReadHeader();
                    string[] headerRow = csv.Context.HeaderRecord;

                    while (csv.Read())
                    {
                        var d = new Dictionary<string, string>();
                        foreach (var column in headerRow)
                            d.Add(column, csv[column]);
                        records.Add(d);
                    }
                    var progress = new Progress<int>(v =>
                    {
                        // This lambda is executed in context of UI thread,
                        // so it can safely update form controls
                        progressBar1.Value = v;
                    });

                    Task.Run(() => { CreateMaterials(records, Path.GetDirectoryName(filedialog.FileName), progress); });
                }


            }
        }

        private dynamic GetFields(string folderId)
        {
            if(!folderFields.ContainsKey(folderId))
            {
                var client = new RestClient(ApiServer);
                var request = new RestRequest("/api/" + companyName + $"/Style/FolderSchema?folderId={folderId}", Method.GET);
                request.AddHeader("Authorization", "Bearer " + accessToken);
                var response = client.Execute<dynamic>(request);
                folderFields.Add(folderId, JsonConvert.DeserializeObject<dynamic>(response.Content));
            }

            return folderFields[folderId];

        }

        private void CreateMaterials(List<Dictionary<string, string>> materials, string current_dir, IProgress<int> progress)
        {
            accessToken = Auth.RefreshAccessToken(authUrl, clientId, clientSecret, Properties.Settings.Default["BeproductRefreshToken"].ToString());

            var client = new RestClient(ApiServer);
            var request = new RestRequest("/api/" + companyName + "/Material/Folders", Method.GET);
            request.AddHeader("Authorization", "Bearer " + accessToken);
            var response = client.Execute<dynamic>(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);

            foreach (var f in result)
                folders.Add( f.name.ToString().ToLower(),  f.id.ToString());


            //Getting folder schema ...

            Dictionary<string, string> prev_row = null;
            PostModel material = null;

            int counter = 1;
            string main_image = null, detail_image = null;
            var color_images = new List<(string filename, string colorid)>();
            string folderName = null;

           
            foreach (var material_row in materials)
            {
                progress?.Report(100 * counter++ / materials.Count);

                if (material_row.First().Value == prev_row?.First().Value && material_row != materials.Last())
                {
                    //populating material object to be posted to beproduct
                    ProcessRow(material_row, ref material, ref color_images);
                }
                else
                {
                    //We are in the row that starts new material
                    if (material != null)
                        CreateMaterial(material, folderName,  main_image, detail_image, color_images, current_dir);

                    material = new PostModel { SizeAndColor = new PostModel.MaterialSizeAndColors { Colorways = new List<ExpandoObject>(), Sizes = new List<PostModel.MaterialSizeAndColors.SizeValue>() }, Suppliers = new List<PostModel.Supplier>() };
                    main_image = null;
                    detail_image = null;

                    folderName = material_row.FirstOrDefault(f => f.Key.ToLower() == "folder").Value;
                    if (!folders.ContainsKey(folderName.ToLower()))
                        continue;
                    var mat_fields = new List<PostModel.Field> { new PostModel.Field { id = "active", value = true }, new PostModel.Field { id = "version", value = "1" } };

                    foreach (var f in material_row)
                    {
                        string field_id = ((IEnumerable<dynamic>)GetFields(folders[folderName.ToLower()])).FirstOrDefault(fld => fld.fieldName?.ToString()?.ToLower() == f.Key?.ToLower())?.fieldId?.ToString();
                        if (!string.IsNullOrEmpty(field_id))
                            mat_fields.Add(new PostModel.Field { id = field_id, value = f.Value });                      
                    }

                    ProcessRow(material_row, ref material, ref color_images);

                    material.fields = mat_fields.ToArray();
                    main_image = material_row.FirstOrDefault(f => f.Key?.ToLower() == "main image").Value?.ToString();
                    detail_image = material_row.FirstOrDefault(f => f.Key?.ToLower() == "detail image").Value?.ToString();
                }

                prev_row = material_row;
            }

            MessageBox.Show("Material import finished!");

        }


        private void CreateMaterial(PostModel material, string folderName, string mainImage, string detailImage, List<(string filename, string colorid)> colorImages, string currentDir)
        {
            var client = new RestClient(ApiServer);

            //create new object and post prev
            if (material != null)
            {
                // Creating a new material 
                var request = new RestRequest("/api/" + companyName + $"/Material/Header?folderId={folders[folderName.ToLower()]}", Method.POST);
                request.AddHeader("Authorization", "Bearer " + accessToken);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(material);
                var response = client.Execute<dynamic>(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK || response.Content.Contains("error"))
                {
                    MessageBox.Show("Could not create material " + material.fields.First(f => f.id == "header_number").value);
                }
                else
                {
                    var post_result = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    string main_image_filename = null;
                    if (!string.IsNullOrEmpty(mainImage) && File.Exists(Path.Combine(currentDir, mainImage)))
                        main_image_filename = Path.Combine(currentDir, mainImage);
                    if (!string.IsNullOrEmpty(main_image_filename))
                        UploadImage(post_result.id?.ToString(), "main", main_image_filename);



                    string detail_image_filename = null;
                    if (!string.IsNullOrEmpty(detailImage) && File.Exists(Path.Combine(currentDir, detailImage)))
                        detail_image_filename = Path.Combine(currentDir, detailImage);
                    if (!string.IsNullOrEmpty(detail_image_filename))
                        UploadImage(post_result.id?.ToString(), "detail", detail_image_filename);


                    foreach (var im in colorImages)
                    {
                        string im_filename = null;

                        if (!string.IsNullOrEmpty(im.filename) && File.Exists(Path.Combine(currentDir, im.filename)))
                            im_filename = Path.Combine(currentDir, im.filename);

                        if (!string.IsNullOrEmpty(im_filename))
                            UploadColorImage(post_result.id?.ToString(), im.colorid, im_filename);
                    }

                    colorImages = new List<(string filename, string colorid)>();
                }
            }
        }
        
        private void ProcessRow(Dictionary<string, string> materialRow, ref PostModel material, ref List<(string filename, string colorid)> colorImages)
        {
            bool color_set = false, material_set = false;
            foreach (var f in materialRow)
            {
                if (!color_set && new string[] { "color number", "color name", "color image", "color hex" }.Contains(f.Key.ToLower()) && !string.IsNullOrEmpty(f.Value))
                {
                    color_set = true;
                    var exp = new ExpandoObject() as IDictionary<string, object>;
                    exp["color_number"] = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "color number").Value?.ToString();
                    exp["color_name"] = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "color name").Value?.ToString();
                    exp["Primary_Color"] = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "color hex").Value?.ToString()?.ToLower().Trim('#');
                    exp["Id"] = Guid.NewGuid().ToString();
                    material.SizeAndColor.Colorways.Add((ExpandoObject)exp);

                    var color_im = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "color image");
                    if (!color_im.Equals(default(KeyValuePair<string, string>)))
                        colorImages.Add((filename: color_im.Value, colorid: exp["Id"].ToString()));
                }
                if (!material_set && new string[] { "size name", "size price", "size uom", "size currency" }.Contains(f.Key.ToLower()) && !string.IsNullOrEmpty(f.Value))
                {
                    material_set = true;
                    material.SizeAndColor.Sizes.Add(new PostModel.MaterialSizeAndColors.SizeValue
                    {
                        Name = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "size name").Value?.ToString(),
                        Currency = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "size currency").Value?.ToString(),
                        Price = string.IsNullOrEmpty(materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "size price").Value) ? 0 : decimal.Parse(materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "size price").Value),
                        Visible = true,
                        UOM = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "size name").Value?.ToString()
                    });

                }
                if (f.Key.ToLower() == "supplier name" && !string.IsNullOrEmpty(f.Value))
                {
                    material.Suppliers.Add(new PostModel.Supplier
                    {
                        Address = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier address").Value?.ToString(),
                        City = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier city").Value?.ToString(),
                        Country = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier country").Value?.ToString(),
                        Fax = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier fax").Value?.ToString(),
                        Name = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier name").Value?.ToString(),
                        Phone = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier phone").Value?.ToString(),
                        State = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier state").Value?.ToString(),
                        Website = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier website").Value?.ToString(),
                        SupplierType = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier type").Value?.ToString(),
                        Zip = materialRow.FirstOrDefault(mf => mf.Key.ToLower() == "supplier zip").Value?.ToString()

                    });
                }
            }
        }


        private void UploadImage(string materialId, string side, string fileName)
        {
            var client = new RestClient(ApiServer);

            var request = new RestRequest("/api/" + companyName + "/Material/SideImageUpload?materialId=" + materialId + "&side=" + side, Method.POST);
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("FileName", Path.GetFileName(fileName));
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddParameter("Test", "test");
            request.AddFile("fileData", fileName);
            var response = client.Execute<dynamic>(request);
            var result = JsonConvert.DeserializeObject(response.Content);

        }


        private void UploadColorImage(string materialId, string colorId, string fileName)
        {
            var client = new RestClient(ApiServer);

            var request = new RestRequest("/api/" + companyName + "/Material/ColorImageUpload?materialId=" + materialId + "&colorid=" + colorId, Method.POST);
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddHeader("FileName", Path.GetFileName(fileName));
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddParameter("Test", "test");
            request.AddFile("fileData", fileName);
            var response = client.Execute<dynamic>(request);
            var result = JsonConvert.DeserializeObject(response.Content);

        }
    

    }
}
