using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Windows.Data.Json;
using Windows.ApplicationModel.Core;

namespace LibRPi
{
    public sealed class HelperClass
    {
        static string API_URL_SHUTDOWN = "http://localhost:8080/api/control/shutdown";
        static string API_URL_RESTART = "http://localhost:8080/api/control/restart";
        static string API_URL_TASKMANAGER = "http://localhost:8080/api/taskmanager/start?appid=";
        static string API_URL_INSTALLED = "http://localhost:8080/api/appx/installed";

        static string PACKAGE_RELATIVE_ID = "PackageRelativeId";
        static string INSTALLED_PACKAGES = "InstalledPackages";

        static string NAME = "Name";
        static string POST = "POST";

        public async void Shutdown() { await PostAsync(API_URL_SHUTDOWN); }

        public async void Restart()  { await PostAsync(API_URL_RESTART); }

        public void AppExit() { CoreApplication.Exit(); }

        public async void StartApp(string name)
        {
            string ID = await GetPackageRelativeId(name);
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(ID);
            string base64 = System.Convert.ToBase64String(toEncodeAsBytes);
            await PostAsync(API_URL_TASKMANAGER + base64);
        }

        private async Task<string> GetPackageRelativeId(string package)
        {
            return await GetNamedString(package);
        }

        private async Task<string> GetNamedString(string packageName)
        {
            JsonObject data = null;
            string getNamedString = null;
            StreamReader reader = await GetAsync(API_URL_INSTALLED);
            
            try
            {
                string json = reader.ReadToEnd();
                data = (JsonObject)JsonObject.Parse(json);
                JsonArray packages = data.GetNamedArray(INSTALLED_PACKAGES);

                for (uint idx = 0; idx < packages.Count; idx++)
                {
                    JsonObject package = packages.GetObjectAt(idx).GetObject();
                    string name = package.GetNamedString(NAME);
                    if (name.ToLower().CompareTo(packageName.ToLower()) == 0)
                    {
                        getNamedString = ((JsonObject)package).GetNamedString(PACKAGE_RELATIVE_ID);
                        break;
                    }
                }
            }
            catch (Exception) {}
            return getNamedString;
        }

        private async Task<StreamReader> PostAsync(string URL) { return await HttpAsync(URL, POST); }

        private async Task<StreamReader> GetAsync(string URL) { return await HttpAsync(URL, null); }

        private async Task<StreamReader> HttpAsync(string URL, string method)
        {
            HttpWebRequest request = null;
            Stream stream = null;
            StreamReader reader = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(URL);
                request.Credentials = new NetworkCredential(Access.User, Access.Password);
                if (method != null) request.Method = method;       

                HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    stream = response.GetResponseStream();
                    reader = new StreamReader(stream);
                }
            }
            catch (Exception) { }
            return reader;
        }
    }
}
