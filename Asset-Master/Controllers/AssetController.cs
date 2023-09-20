using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Asset_Master.Entities;
using Asset_Master.Interfaces;

namespace Asset_Master.Controllers;



[ApiController]
[Route("api/[controller]")]
public class AssetController : Controller
{
    private readonly IAsset _assets;
    private IMapper _mapper;

    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public AssetController(IAsset assets, IMapper mapper, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)//ISharePointService sharePointService
    {
        _assets = assets;
        _mapper = mapper;
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }
    [HttpGet]
    [Route("GetAllAsset")]
    public string NewGetAllassetsJobs()
    {
        //Recurring Jobs
        //Recurring jobs fire many times on the specified CRON schedule.
        _recurringJobManager.AddOrUpdate("jobId2", () => GetAllassets(), Cron.MinuteInterval(1440)); //Cron.Minutely());



        return "The Recurring Job will run for getting the data From MariaDb and sync the same data to the Sharepoint List!";
    }




    [NonAction]
    public async Task<IActionResult> GetAllassets()
    {
        IEnumerable<assets> assets = await _assets.GetAllassets();

        // Connect to SharePoint and perform the check and insert operations
        string clientId = "05d111d1-e632-40e0-803b-0976c6025430";
        string clientSecret = "bLs8Q~tNx~HEfY6saAQDEUuz9XH80MwBb2Fdidc-";
        string tenantId = "7bf109b7-39a2-49d4-911d-09736db83214";

        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithTenantId(tenantId)
            .Build();

        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

        try
        {
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            string siteId = "2741d2aa-86e3-45da-81de-532088acaadb";
            string listId = "36ba7abb-a095-4023-8839-64eb2186dc11"; // Replace with the actual ID of your SharePoint list

            // Read Operation: Get all items from the list
            var listItems = await graphClient.Sites[siteId].Lists[listId]
                .Items
                .Request()
                .Select("id,fields")
                .Expand("fields")
                .GetAsync();




            // Read Operation: Get all items from the list
            const int batchSize = 100;
            List<ListItem> allItems = new List<ListItem>();
            var request = graphClient.Sites[siteId].Lists[listId].Items.Request().Top(batchSize).Select("id,fields").Expand("fields");

            try
            {
                do
                {
                    var batchItems = await request.GetAsync();
                    allItems.AddRange(batchItems.CurrentPage);

                    if (batchItems.NextPageRequest == null)
                    {
                        // No more items, break out of the loop
                        break;
                    }

                    request = batchItems.NextPageRequest;
                } while (true);
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error accessing SharePoint list: {ex.Message}");
            }





            // Fetch the unique identifiers (e.g., Email) of the existing items in SharePoint
            var existingIdentifiers = allItems.Select(item =>
            {
                var fields = item.Fields;
                if (fields.AdditionalData.TryGetValue("ToolID", out var identifierValue))
                {
                    if (double.TryParse(identifierValue.ToString(), out var doubleValue))
                    {
                        return ((int)doubleValue).ToString();
                    }
                    // Handle other types if needed
                }
                return null;
            }).Where(identifier => identifier != null).ToList();

            int newItemsCount = 0;

            // Check each asset in the collection
            foreach (var asset in assets)
            {
                string uniqueIdentifier = asset.id.ToString(); // Replace with the actual unique identifier property from the asset

                // Check if the unique identifier already exists in SharePoint
                if (!existingIdentifiers.Contains(uniqueIdentifier))
                {
                    // Create Operation: Create a new list item
                    var newItem = new ListItem
                    {
                        Fields = new FieldValueSet
                        {
                            AdditionalData = new Dictionary<string, object>
                        {
                            { "ToolID", uniqueIdentifier }, // Replace "YourEmailInternalName" with the actual internal name of the Email field in SharePoint
                            { "Title", asset.name },
                            { "Assettag", asset.asset_tag},
                            { "ModelID", asset.model_id },
                            { "Serial", asset.serial},
                            { "Assignedto", asset.assigned_to},
                            { "User_id", asset.user_id},
                            { "Status_id", asset.status_id},
                            { "snipeit_workstation", asset._snipeit_workstation_1},
                            // Add other fields as needed
                        }
                        }
                    };
                    
                    var createdItem = await graphClient.Sites[siteId].Lists[listId].Items.Request().AddAsync(newItem);
                    Console.WriteLine("Created Item ID: " + createdItem.Id);

                    newItemsCount++;
                }
                else
                {

                    // Assuming you have retrieved the items from SharePoint
                    string siteId1 = "2741d2aa-86e3-45da-81de-532088acaadb";
                    string listId1 = "36ba7abb-a095-4023-8839-64eb2186dc11";
                    string emailColumnInternalName = "ToolID";
                    string emailToFilter = asset.id.ToString();
                    //string newEmailValue = "kajedemail@example.com"; // Replace with the new email value you want to set

                    // Set the 'Prefer' header to allow filtering on non-indexed columns (use with caution)
                    var requestOptions = new List<HeaderOption>
                    {
                        new HeaderOption("Prefer", "HonorNonIndexedQueriesWarningMayFailRandomly")
                    };

                    // Query the SharePoint list to find the item with the ToolID "kavitha"
                    var listItems1 = await graphClient.Sites[siteId].Lists[listId]
                        .Items
                        .Request(requestOptions)
                        .Filter($"fields/{emailColumnInternalName} eq '{emailToFilter}'")
                        .GetAsync();

                    // Assume there's only one item with the email "kavitha" (since it's unique)
                    var itemToUpdate = listItems1.FirstOrDefault();

                    if (itemToUpdate != null)
                    {
                        // Get the ID of the item with the ToolID "kavitha"
                        string itemId = itemToUpdate.Id;

                        // Update the "Email" field of the item with the new email value
                        var updateItem = new ListItem
                        {
                            Fields = new FieldValueSet
                            {
                                AdditionalData = new Dictionary<string, object>
                    {
                        { "ToolID", asset.id.ToString()}, // Replace "YourEmailInternalName" with the actual internal name of the Email field in SharePoint
                        { "Title", asset.name },
                        { "Assettag", asset.asset_tag},
                        { "ModelID", asset.model_id },
                        { "Serial", asset.serial},
                        { "Assignedto", asset.assigned_to},
                        { "User_id", asset.user_id},
                        { "Status_id", asset.status_id},
                        { "snipeit_workstation", asset._snipeit_workstation_1},
                        // Add other fields as needed
                    }
                            }
                        };

                        // Save the changes back to SharePoint using the item's ID
                        await graphClient.Sites[siteId1].Lists[listId1].Items[itemId].Request().UpdateAsync(updateItem);
                        Console.WriteLine("Item updated successfully.");
                    }
                    else
                    {
                        // The item with the email "kavitha" does not exist in SharePoint.
                        // You may choose to insert a new item instead or perform any other action.
                        Console.WriteLine($"Item with ToolID '{emailToFilter}' not found in SharePoint.");
                    }





                }
            }

            Console.WriteLine($"Inserted {newItemsCount} new items into SharePoint.");

            return Ok(assets); // Return the assets as the response
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error accessing SharePoint list: {ex.Message}");
            return StatusCode(500); // Return an error status code if there's an issue with SharePoint
        }
        
        return Ok(assets);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetassetsById(int id)
    {
        assets assets = await _assets.GetassetsById(id);
        return Ok(assets);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Updateassets(int id, Createassets model)
    {
        await _assets.Updateassets(id, model);
        return Ok("The assets was successfully updated in the database");
    }
    


    [HttpGet]
    [Route("SharepointToMariaDB")]
    public string RecurringJobs()
    {
        //Recurring Jobs
        //Recurring jobs fire many times on the specified CRON schedule.
        _recurringJobManager.AddOrUpdate("jobId", () => MinutelyJobMessageAsync(), Cron.MinuteInterval(5)); //Cron.Minutely());



        return "The Recurring Job Will run for checking the sharepoint list, if there is any changes on that, that will update into MariaDB!";
    }

    [NonAction]
    public async Task<string> MinutelyJobMessageAsync()
    {
        string clientId = "05d111d1-e632-40e0-803b-0976c6025430";
        string clientSecret = "bLs8Q~tNx~HEfY6saAQDEUuz9XH80MwBb2Fdidc-";
        string tenantId = "7bf109b7-39a2-49d4-911d-09736db83214";

        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithTenantId(tenantId)
            .Build();

        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

        try
        {
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            string siteId = "2741d2aa-86e3-45da-81de-532088acaadb";
            string listId = "36ba7abb-a095-4023-8839-64eb2186dc11"; // Replace with the actual ID of your SharePoint list

            var listItems = await graphClient.Sites[siteId].Lists[listId]
                .Items
                .Request()
                .Select("id,fields")
                .Expand("fields")
                .Filter("fields/ChangesStatus eq 'yes'") // Items where ChangesStatus is not 'rencata'
                .GetAsync();


            // Process and print the list items
            foreach (var item in listItems)
            {
                var fields = item.Fields;


                int id = ParseToInt(fields.AdditionalData.ContainsKey("ToolID") ? fields.AdditionalData["ToolID"] : null);
                int assignedto = ParseToInt(fields.AdditionalData.ContainsKey("Assignedto") ? fields.AdditionalData["Assignedto"] : null);
                int modelid = ParseToInt(fields.AdditionalData.ContainsKey("ModelID") ? fields.AdditionalData["ModelID"] : null);
                int userid = ParseToInt(fields.AdditionalData.ContainsKey("User_id") ? fields.AdditionalData["User_id"] : null);
                int statusid = ParseToInt(fields.AdditionalData.ContainsKey("Status_id") ? fields.AdditionalData["Status_id"] : null);
                string aserial = fields.AdditionalData["Serial"].ToString();
                string aname = fields.AdditionalData["Title"].ToString();

                string msnipet = fields.AdditionalData.ContainsKey("snipeit_workstation") ? fields.AdditionalData["snipeit_workstation"].ToString() : "";

                var model = new Createassets
                {
                    // ... set other properties
                    name = aname,
                    serial= aserial,
                    assigned_to = assignedto,
                    model_id = modelid,
                    user_id = userid,
                    status_id = statusid,
                    _snipeit_workstation_1 = msnipet
                };
                await _assets.Updateassets(id, model);
                Console.WriteLine("Item ID: " + item.Id);


                if (item.Id != null)
                {
                    // Get the ID of the item with the ToolID "kavitha"
                    string itemId = item.Id;

                    // Update the "Email" field of the item with the new email value
                    var updateItem = new ListItem
                    {
                        Fields = new FieldValueSet
                        {
                            AdditionalData = new Dictionary<string, object>
                    {
                        { "ChangesStatus", "No"}, // Replace "YourEmailInternalName" with the actual internal name of the Email field in SharePoint
                         { "Status_id","5"}// Add other fields as needed
                    }
                        }
                    };

                    // Save the changes back to SharePoint using the item's ID
                    await graphClient.Sites[siteId].Lists[listId].Items[itemId].Request().UpdateAsync(updateItem);
                    Console.WriteLine("Item updated successfully.");
                }
                else
                {
                    // The item with the email "kavitha" does not exist in SharePoint.
                    // You may choose to insert a new item instead or perform any other action.
                    Console.WriteLine($"Item with ToolID '{item.Id}' not found in SharePoint.");
                }




                // Access the 'fields' object
                //var fields = item.Fields;
                if (fields != null)
                {
                    // Access the 'Email' field using the internal name
                    if (fields.AdditionalData.ContainsKey("ToolID")) // Replace "YourEmailInternalName" with the internal name of the Email field in SharePoint
                    {
                        Console.WriteLine("Item Email: " + fields.AdditionalData["ToolID"]);
                    }
                }
            }
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error accessing SharePoint list: {ex.Message}");
        }

        return "Welcome Minutely in Continuos Job Demo!";
    }



    // Define the ParseToInt method in a suitable scope (e.g., in the same class or a relevant utility class)
    private int ParseToInt(object? value)
    {
        if (value != null)
        {
            double doubleValue;
            if (double.TryParse(value.ToString(), out doubleValue))
            {
                return (int)doubleValue;
            }
        }
        // Handle the case where the conversion fails or the value is null
        // You can return a default value or throw an exception, depending on your requirements.
        return 0; // Default value for failure
    }

}




