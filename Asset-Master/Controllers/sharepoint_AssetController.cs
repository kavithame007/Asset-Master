﻿using Asset_Master.Entities;
using Asset_Master.Interfaces;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Auth;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Linq;

namespace Asset_Master.Controllers;

   
[ApiController]
[Route("api/[controller]")]
public class sharepoint_AssetController : Controller
{
    private readonly Isharepoint_Asset _sassets;
    private IMapper _mapper;

    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public sharepoint_AssetController(Isharepoint_Asset sassets, IMapper mapper, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)//ISharePointService sharePointService
    {
        _sassets = sassets;
        _mapper = mapper;
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }




    [HttpGet]
    [Route("GetAllAvailableAsset")]
    public string NewRecurringJobs()
    {
        //Recurring Jobs
        //Recurring jobs fire many times on the specified CRON schedule.
        _recurringJobManager.AddOrUpdate("jobId1", () => GetAllavailableassets(), Cron.MinuteInterval(5)); //Cron.Minutely());
        return "The Recurring Job will run for getting the Available Asset from MariaDB and Write the same to the Sharepoint List!";
    }



    [HttpGet]
    public async Task<IActionResult> GetAllavailableassets()
    {
        IEnumerable<sharepoint_Asset> sassets = await _sassets.GetAllavailableassets();





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
            string listId = "c805035c-357d-4f82-ba42-416b7108d1de"; // Replace with the actual ID of your SharePoint list

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
                if (fields.AdditionalData.TryGetValue("AssetTag", out var identifierValue))
                {
                    return identifierValue.ToString(); // Convert it to string directly
                }
                return null;
            }).Where(identifier => identifier != null).ToList();


            int newItemsCount = 0;

            // Check each asset in the collection
            foreach (var asset in sassets)
            {
                string uniqueIdentifier = asset.asset_tag.ToString(); // Replace with the actual unique identifier property from the asset

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
                             // Replace "YourEmailInternalName" with the actual internal name of the Email field in SharePoint
                            { "Title", asset.name },
                            { "AssetTag", asset.asset_tag},
                            { "ModelName", asset.modelname },
                            { "CategoryName", asset.categoryname},
                            {"serial", asset.serial },
                            
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
                    string listId1 = "c805035c-357d-4f82-ba42-416b7108d1de";
                    string emailColumnInternalName = "AssetTag";
                    string emailToFilter = asset.asset_tag.ToString();
                    //string newEmailValue = "kajedemail@example.com"; // Replace with the new email value you want to set

                    // Set the 'Prefer' header to allow filtering on non-indexed columns (use with caution)
                    var requestOptions = new List<HeaderOption>
                    {
                        new HeaderOption("Prefer", "HonorNonIndexedQueriesWarningMayFailRandomly")
                    };

                    // Query the SharePoint list to find the item with the AssetTag "kavitha"
                    var listItems1 = await graphClient.Sites[siteId].Lists[listId]
                        .Items
                        .Request(requestOptions)
                        .Filter($"fields/{emailColumnInternalName} eq '{emailToFilter}'")
                        .GetAsync();

                    // Assume there's only one item with the email "kavitha" (since it's unique)
                    var itemToUpdate = listItems1.FirstOrDefault();

                    if (itemToUpdate != null)
                    {
                        // Get the ID of the item with the AssetTag "kavitha"
                        string itemId = itemToUpdate.Id;

                        // Update the "Email" field of the item with the new email value
                        var updateItem = new ListItem
                        {
                            Fields = new FieldValueSet
                            {
                                AdditionalData = new Dictionary<string, object>
                    {
                        // Replace "YourEmailInternalName" with the actual internal name of the Email field in SharePoint
                        { "Title", asset.name },
                        { "AssetTag", asset.asset_tag},
                        { "ModelName", asset.modelname },
                        { "CategoryName", asset.categoryname},
                        {"serial", asset.serial },
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
                        Console.WriteLine($"Item with AssetTag '{emailToFilter}' not found in SharePoint.");
                    }
                }
            }

            Console.WriteLine($"Inserted {newItemsCount} new items into SharePoint.");


            int deleteCount = 0;

            // Check each asset in the collection
            foreach (var exasset in allItems)
            {
                var fields = exasset.Fields;
                string uniqueIdentifier = fields.AdditionalData["AssetTag"].ToString(); // Replace with the actual unique identifier property from the asset

                // Check if the unique identifier already exists in SharePoint
                if (sassets.Any(asset => asset.asset_tag == uniqueIdentifier))
                {
                    Console.WriteLine("none");
                }
                else
                {
                    // Create Operation: Create a new list item

                    string itemID = fields.Id;
                    //(string)fields.AdditionalData["ID"];
                    await graphClient.Sites[siteId].Lists[listId].Items[itemID].Request().DeleteAsync();
                    Console.WriteLine("Created Item ID: " + itemID);

                    deleteCount++;
                }
            }





            return Ok(sassets); // Return the assets as the response
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error accessing SharePoint list: {ex.Message}");
            return StatusCode(500); // Return an error status code if there's an issue with SharePoint
        }








        return Ok(sassets);
    }

}




