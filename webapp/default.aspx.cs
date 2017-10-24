using Amazon.S3;
using System;

namespace webapp
{
  public partial class _default : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {
      IAmazonS3 client = new AmazonS3Client(Amazon.RegionEndpoint.EUWest1);
      var list = client.ListBuckets();
    }
  }
}