---
services: app-service\mobile, app-service
platforms: dotnet, windows
author: ggailey777
---
# App Service Mobile Apps completed quickstart for Windows apps
This repository contains Windows app projects based on the App Service Mobile Apps quickstart project, which you can download from the [Azure portal](https://portal.azure.com). These projects have been enhanced by the addition of offline sync, authentication, and push notification functionality. This demonstrates how to best integrate the various Mobile Apps features. To learn how to download the Windows quickstart app project from the portal, see [Create a Windows app](https://azure.microsoft.com/documentation/articles/app-service-mobile-windows-store-dotnet-get-started/). 

This project was cloned from [here](https://github.com/Azure-Samples/app-service-mobile-windows-quickstart)

This readme topic contains the following information to help you run the sample app project and to better understand the design decisions.

+ [Overview](#overview)
+ [Configure the Mobile App backend](#configure-the-mobile-app-backend)
+ [Configure the Windows app](#configure-the-windows-app)
	+ [Set the Mobile App backend URL](#set-the-mobile-app-backend-url)
	+ [Install the SQLite runtime for Windows](#install-the-sqlite-runtime-for-windows)
	+ [Configure authentication](#configure-authentication)
	+ [Configure push notifications](#configure-push-notifications)
+ [Running the app](#running-the-app)
+ [Implementation notes](#implementation-notes)
	+ [Template push notification registration](#template-push-notification-registration)
	+ [Push to an authenticated user](#push-to-an-authenticated-user)
	+ [Client-added push notification tags](#client-added-push-notification-tags)
	+ [Authenticate first](#authenticate-first)
	+ [Check for expired tokens](#check-for-expired-tokens)

##Overview
The projects in this repository are equivalent to downloading the quickstart Windows app project from the portal and then completing the following Mobile Apps tutorials:

+ [Enable offline sync for your Windows app](https://azure.microsoft.com/documentation/articles/app-service-mobile-windows-store-dotnet-get-started-offline-data/)
+ [Add authentication to your Windows app](https://azure.microsoft.com/en-us/documentation/articles/app-service-mobile-windows-store-dotnet-get-started-users/)
+ [Add push notifications to your Windows app](https://azure.microsoft.com/en-us/documentation/articles/app-service-mobile-windows-store-dotnet-get-started-push/) 

The Universal Windows Platform (UWP) project for Windows 10 requires Visual Studio 2015.

## Configure the Mobile App backend

Before you can use this sample, you must have created and published a Mobile App backend project that supports  both authentication and push notifications (the backend supports offline sync by default). You can do this either by completing the previously indicated tutorials, or you can use one of the following Mobile Apps backend projects:

+ [.NET backend quickstart project for Mobile Apps](https://github.com/azure-samples/app-service-mobile-dotnet-backend-quickstart)
+ [Node.js backend quickstart project for Mobile Apps](https://github.com/azure-samples/app-service-mobile-nodejs-backend-quickstart)

The readme file in this project will direct you to create a new Mobile App backend in App Service, then download, modify, and publish project to App Service.

After you have your new Mobile App backend running, you can configure this project to connect to that new backend.

## Configure the Windows app

The app project has offline sync support enabled, along with authentication and push notifications. However, you need to configure the project, including authentication and push notifications, before the app will run properly.

### Set the Mobile App backend URL

The first thing you need to do is to set the URL of your Mobile App backend in the **MobileServiceClient** constructor. To do this, open the shared App.xaml.cs project file, locate the **MobileServiceClient** constructor and replace the URL with the URL of your Mobile App backend.
 
### Install the SQLite runtime for Windows

Although the SQLite NuGet packages are already installed in the project, you must make sure that the SQLite runtimes are also available on your local computer: 

* **Windows 10:** Install [SQLite for the Windows Universal Platform](http://sqlite.org/2016/sqlite-uwp-3120200.vsix).
* **Windows 8.1 Runtime:** Install [SQLite for Windows 8.1](http://go.microsoft.com/fwlink/?LinkID=716919).
* **Windows Phone 8.1:** Install [SQLite for Windows Phone 8.1](http://go.microsoft.com/fwlink/?LinkID=716920).

### Configure authentication

Because both the client and backend are configured to use authentication, you must define an authentication provider for your app and register it with your Mobile App backend in the [portal](https://portal.azure.com).

1. Follow the instructions in the topic to configure the Mobile App backend to use one of the following authentication providers:

	+ [AAD](https://azure.microsoft.com/documentation/articles/app-service-mobile-how-to-configure-active-directory-authentication/)
	+ [Facebook](https://azure.microsoft.com/documentation/articles/app-service-mobile-how-to-configure-facebook-authentication/)
	+ [Google](https://azure.microsoft.com/documentation/articles/app-service-mobile-how-to-configure-google-authentication/)
	+ [Microsoft account](https://azure.microsoft.com/documentation/articles/app-service-mobile-how-to-configure-microsoft-authentication/)
	+ [Twitter](https://azure.microsoft.com/documentation/articles/app-service-mobile-how-to-configure-twitter-authentication/)

2. By default, the app is configured to use server-directed Microsoft Account authentication. To use a different authentication provider, change the provider by locating this line of code in the shared MainPage.xaml.cs file and changing it to one of these other values: `AAD`, `Google`, `Facebook`, or `Twitter`.

		var provider = "Facebook";

### Configure push notifications

You need to configure push notifications by registering your Windows app with the Windows Store then storing the app's package SID and client secret in the Mobile App backend. These credentials are used by Azure to connect to Windows Notification Service (WNS) to send push notifications. Assuming that you have already created the notification hub in your backend, complete the following sections of the push notifications tutorial to configure push notifications:

1. [Register your app for push notifications](https://github.com/Azure/azure-content/blob/master/includes/app-service-mobile-register-wns.md)
2. [Configure the backend to send push notifications](https://github.com/Azure/azure-content/blob/master/includes/app-service-mobile-configure-wns.md)

## Running the app

With both the Mobile App backend and the app configured, you can run the app project.

1. Right-click the Windows Store project, click **Set as StartUp Project**, then press the F5 key to run the Windows Store app.

2. In the app, click the **Sign-in** button and authenticate with the provider. 
	
	After authentication succeeds, the device is registered for push notifications and any existing data is downloaded from Azure.

2. Stop the Windows Store app and repeat the previous steps for the Windows Phone Store app.

	At this point, both devices are registered to receive push notifications.

3. Run the Windows Store app again, and type text in **Insert a TodoItem**, and then click **Save**.

   	Note that after the insert completes, both the Windows Store and the Windows Phone apps receive a push notification from WNS. The notification is displayed on Windows Phone even when the app isn't running.


## Implementation notes 
This section highlights changes made to the original tutorial samples and other design decisions were made when implementing all of the features or Mobile Apps in the same client app. 

###Template push notification registration
The original push notification tutorial used a native WNS registration. This sample has been changed to use a template registration, which makes it easier to send push notifications to users on multiple clients from a single **send** method call. The following code defines the toast template registration:

    // Define a toast templates for WNS.
    var toastTemplate =
        @"<toast><visual><binding template=""ToastText02""><text id=""1"">"
                        + @"New item:</text><text id=""2"">"
                        + @"$(message)</text></binding></visual></toast>";

    JObject templateBody = new JObject();
    templateBody["body"] = toastTemplate;

    // Add the required WNS toast header.
    JObject wnsToastHeaders = new JObject();
    wnsToastHeaders["X-WNS-Type"] = "wns/toast";
    templateBody["headers"] = wnsToastHeaders;

    JObject templates = new JObject();
    templates["testTemplate"] = templateBody;


For more information, see [How to: Register push templates to send cross-platform notifications](https://azure.microsoft.com/documentation/articles/app-service-mobile-dotnet-how-to-use-client-library/#how-to-register-push-templates-to-send-cross-platform-notifications).

###Push to an authenticated user
Because the user is authenticated before push registration occurs, the user ID is automatically added as a tag in the installation. The backend then uses this tag to send push notifications only to devices registered to the user doing the insert. For more information, see the readme file for the quickstart completed backend project. 

###Client-added push notification tags

When a mobile app registers for push notifications using an Azure App Service Mobile Apps backend, there are two default tags that can get added to the registration in Azure Notification Hubs: the installation ID, which is unique to the app on a given device, and the user ID, which is only added when the user has been previously authenticated. Any other tags that get supplied by the client are ignored, which is by design. (Note that this differs from Mobile Services, where the client could supply any tag and there were hooks into the registration process on the backend to validate tags on incoming registrations.) 

Because the client can’t add tags and at the same time there is not service-side hooks into the push notification registration process, the client needs to do the work of adding new tags to a given registration. In this sample, an `/updatetags` endpoint in the backend lets the client add tags to their push registration. The client calls that endpoint to create new tags, as follows:

	// Define two new tags as a JSON array.
	var body = new JArray();
	body.Add("broadcast");
	body.Add("test");
	
	// Call the custom API '/api/updatetags/<installationid>' 
	// with the JArray of tags.
	var response = await App.MobileService
	    .InvokeApiAsync("updatetags/" 
	    + App.MobileService.InstallationId, body);

For more information, see [Adding push notification tags from an Azure Mobile Apps client](http://blogs.msdn.com/b/writingdata_services/archive/2016/01/22/adding-push-notification-tags-from-an-azure-mobile-apps-client.aspx).

###Authenticate first
This sample is a little different from the tutorials in that push notifications are send to all devices with push registrations that belong to a specific user. When an authenticated user registers for push notifications, a tag with the user ID is automatically added. Because of this, it's important to have the user sign-in before registering for push notifications. You should also have the user sign-in before executing any data or sync requests, which will result in an exception when the endpoint requires authentication. You also probably don't want an unauthenticated user to see offline data stored on the device. The following button Click event handler shows how to require explicit user sign-in before push registration and doing the initial data sync:

    private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
    {
        // Login the user and then load data from the mobile app.
        if (await AuthenticateAsync())
        {
            // Register for push notifications.
            InitNotificationsAsync();

            // Hide the login button and load items from the mobile app.
            ButtonLogin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            await InitLocalStoreAsync(); //offline sync support.
            await RefreshTodoItems();
        }
    }

###Check for expired tokens

The [Windows authentication tutorial](https://zure.microsoft.com/documentation/articles/app-service-mobile-windows-store-dotnet-get-started-users/) featured storing App Service-issued tokens to reuse later for authentication without having to call **LoginAsync**. However, these tokens expire so the result could be a 401 response. This sample project has a TokenExtensions.cs project file that implements a **IsTokenExpired** extension method on **MobileServiceClient**. This extension method decodes the cached token and checks the expiration datetime against the current datetime, and returns true when the token is already expired. For more information about this extension method, see the post [Check for expired Azure Mobile Services authentication tokens](http://blogs.msdn.com/b/writingdata_services/archive/2015/04/27/check-for-expired-azure-mobile-services-authentication-tokens.aspx).
