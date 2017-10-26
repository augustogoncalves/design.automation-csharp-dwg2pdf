using Amazon.S3;
using Autodesk.Forge;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System.Net;
using System.Web;

namespace webapp
{
  public partial class _default : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected async void Button1_Click(object sender, EventArgs e)
    {
      // define the Bucket, DWG and PDF file names
      string bucketName = "designautomationsample-" + Guid.NewGuid().ToString();
      string dwgFileName = FileUpload1.FileName;
      string pdfFileName = dwgFileName.Replace(".dwg", ".pdf");

      // upload from client/browser to my server
      string fileSavePath = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data"), dwgFileName);
      Directory.CreateDirectory(Path.GetDirectoryName(fileSavePath));
      FileUpload1.SaveAs(fileSavePath);

      IAmazonS3 client = new AmazonS3Client(Amazon.RegionEndpoint.USWest2);

      // create AWS Bucket
      if (!await client.DoesS3BucketExistAsync(bucketName))
        await client.EnsureBucketExistsAsync(bucketName);

      // Upload file from Server to S3 bucket
      client.UploadObjectFromFilePath(bucketName, FileUpload1.FileName, fileSavePath, null);

      // delete files from server
      Directory.Delete(Path.GetDirectoryName(fileSavePath), true);

      // OAuht 2-legged on Forge
      TwoLeggedApi apiInstance = new TwoLeggedApi();
      dynamic bearer = await apiInstance.AuthenticateAsync(Config.FORGE_CLIENT_ID, Config.FORGE_CLIENT_SECRET, Autodesk.Forge.oAuthConstants.CLIENT_CREDENTIALS, Config.FORGE_SCOPE_DESIGN_AUTOMATION);

      // generate URLs for Design Automation to access (download & upload) S3 files
      Uri downloadFromS3 = new Uri(client.GeneratePreSignedURL(bucketName, dwgFileName, DateTime.Now.AddMinutes(90), null));

      Dictionary<string, object> props = new Dictionary<string, object>();
      props.Add("Verb", "PUT");
      Uri uploadToS3 = new Uri(client.GeneratePreSignedURL(bucketName, pdfFileName, DateTime.Now.AddMinutes(10), props));

      // prepare WorkItem (based on the built-in "PlotToPDF" activity")
      WorkItemsApi workItemsApi = new WorkItemsApi();
      workItemsApi.Configuration.AccessToken = bearer.access_token;
      JObject arguments = new JObject
      {
        new JProperty(
          "InputArguments", new JArray
          {
            new JObject
            {
              new JProperty("Resource", downloadFromS3.GetLeftPart(UriPartial.Path)),
              new JProperty("Headers", MakeHeaders( WebRequestMethods.Http.Get, downloadFromS3)),
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
              new JProperty("Resource", uploadToS3.GetLeftPart(UriPartial.Path)),
              new JProperty("Headers", MakeHeaders(WebRequestMethods.Http.Put, uploadToS3)),
              new JProperty("StorageProvider",  "Generic")
            }
          }
        )
      };

      // submit the workitem...
      dynamic workitem = await workItemsApi.CreateWorkItemAsync(new Autodesk.Forge.Model.WorkItem(string.Empty, arguments, null, null, null, "PlotToPDF"));
      // wait...
      System.Threading.Thread.Sleep(5000); // wait 1 second
      // get the status
      string id = workitem.Id;
      dynamic status = await workItemsApi.GetWorkItemAsync(id);  // Due an issue with the .NET SDK, this is not working (#17)

      // download the PDF from S3 to our server
      fileSavePath = fileSavePath.Replace(".dwg", ".pdf");
      client.DownloadToFilePath(bucketName, pdfFileName, fileSavePath, null);

      // send the PDF file to the client (DO NOT expose a direct URL to S3, but send the bytes)
      Response.Clear();
      Response.ContentType = "application/pdf";
      Response.AddHeader("Content-Disposition", "attachment;filename=\"" + pdfFileName + "\"");
      Response.BinaryWrite(File.ReadAllBytes(fileSavePath));
      Response.Flush();
      Response.End();

      // delete files on S3
      await client.DeleteObjectAsync(bucketName, dwgFileName);
      await client.DeleteObjectAsync(bucketName, pdfFileName);
      await client.DeleteBucketAsync(bucketName);

      // delete PDF file from server
      Directory.Delete(Path.GetDirectoryName(fileSavePath), true);
    }

    private JArray MakeHeaders(string verb, Uri query)
    {
      // Prepare headers
      var collection = AWS.Signature.SignatureHeader(
        Amazon.RegionEndpoint.USWest2, query.Host,
        verb, query.AbsolutePath);

      // organize headers for Design Automation
      JArray headers = new JArray();
      foreach (KeyValuePair<string, string> item in collection)
      {
        headers.Add(new JObject
        {
          new JProperty("Name", item.Key),
          new JProperty("Value", item.Value)
        });
      }

      return headers;
    }
  }
}