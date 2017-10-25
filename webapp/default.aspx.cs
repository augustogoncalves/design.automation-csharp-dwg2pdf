using Amazon.S3;
using Amazon.S3.Model;
using Autodesk.Forge;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace webapp
{
  public partial class _default : System.Web.UI.Page
  {
    private const string BUCKET_NAME = "designautomation-sample";

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected async void Button1_Click(object sender, EventArgs e)
    {
      string dwgFileName = FileUpload1.FileName;
      string pdfFileName = dwgFileName.Replace(".dwg", ".pdf");

      // upload from client/browser to my server
      string fileSavePath = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data"), dwgFileName);
      Directory.CreateDirectory(Path.GetDirectoryName(fileSavePath));
      FileUpload1.SaveAs(fileSavePath);

      // create AWS Bucket
      IAmazonS3 client = new AmazonS3Client(Amazon.RegionEndpoint.EUWest1);

      if (!await client.DoesS3BucketExistAsync(BUCKET_NAME))
        await client.EnsureBucketExistsAsync(BUCKET_NAME);

      // Upload file to S3 bucket
      client.UploadObjectFromFilePath(BUCKET_NAME, FileUpload1.FileName, fileSavePath, null);

      // delete server files
      Directory.Delete(Path.GetDirectoryName(fileSavePath), true);

      // OAuht 2-legged on Forge
      TwoLeggedApi apiInstance = new TwoLeggedApi();
      dynamic bearer = await apiInstance.AuthenticateAsync(Config.FORGE_CLIENT_ID, Config.FORGE_CLIENT_SECRET, Autodesk.Forge.oAuthConstants.CLIENT_CREDENTIALS, Config.FORGE_SCOPE_DESIGN_AUTOMATION);

      var download = client.GeneratePreSignedURL(BUCKET_NAME, dwgFileName, DateTime.Now.AddMinutes(10), null);
      Dictionary<string, object> props = new Dictionary<string, object>();
      props.Add("Verb", "PUT");
      var upload = client.GeneratePreSignedURL(BUCKET_NAME, pdfFileName, DateTime.Now.AddMinutes(10), props);

      // prepare Activity
      WorkItemsApi workItemsApi = new WorkItemsApi();
      workItemsApi.Configuration.AccessToken = bearer.access_token;

      JObject arguments = new JObject
      {
        new JProperty(
          "InputArguments", new JArray
          {
            new JObject
            {
              new JProperty("Resource", download),
              new JProperty("Name",  "HostDwg")
            }
          }
        ),
        new JProperty(
          "OutputArguments", new JArray
          {
            new JObject
            {
              new JProperty("Name", "Result"),
              new JProperty("HttpVerb", "PUT"),
              new JProperty("Resource", upload),
              new JProperty("StorageProvider",  "Generic")
            }
          }
        )
      };

      string id = string.Empty;
      dynamic workitem = await workItemsApi.CreateWorkItemAsync(new Autodesk.Forge.Model.WorkItem(id, arguments, null, null, null, "PlotToPDF"));
      
      System.Threading.Thread.Sleep(5000); // wait 1 second

      id = workitem.Id;
      dynamic status = await workItemsApi.GetWorkItemAsync(id);
            
      fileSavePath = fileSavePath.Replace(".dwg", ".pdf");
      client.DownloadToFilePath(BUCKET_NAME, pdfFileName, fileSavePath, null);

      Response.Clear();
      Response.ContentType = "application/pdf";
      Response.AddHeader("Content-Disposition", "attachment;filename=\"" + pdfFileName + "\"");
      Response.BinaryWrite(File.ReadAllBytes(fileSavePath));
      Response.Flush();
      Response.End();

      // delete file on S3
      await client.DeleteObjectAsync(BUCKET_NAME, dwgFileName);
      await client.DeleteObjectAsync(BUCKET_NAME, pdfFileName);

      Directory.Delete(Path.GetDirectoryName(fileSavePath), true);

      // should be empty
      IList<string> objectsInBucket = await client.GetAllObjectKeysAsync(BUCKET_NAME, string.Empty, null);
    }
  }
}