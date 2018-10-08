# design.automation-chsarp-dwg2pdf

This sample uses [Design Automation v2](https://forge.autodesk.com/en/docs/design-automation/v2) and built-in activity `PlotToPdf` to generate PDFs from DWG files.

# Setup

## Prerequisites

1. **AWS Account**: required to store files and access from Design Automation
2. **Forge Account**: Learn how to create a Forge Account, activate subscription and create an app at [this tutorial](http://learnforge.autodesk.io/#/account/). 
3. **Visual Studio**: Community (Windows).

## Running locally

Specify the keys at the `web.config` file.

```xml
<appSettings file="Web.Keys.Config">
  <add key="AWSProfileName" value=""/>
  <add key="AWSAccessKey" value=""/>
  <add key="AWSSecretKey" value=""/>
  <add key="FORGE_CLIENT_ID" value="" />
  <add key="FORGE_CLIENT_SECRET" value="" />
</appSettings>
```

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

## Written by

Augusto Goncalves [@augustomaia](https://twitter.com/augustomaia), [Forge Partner Development](http://forge.autodesk.com)