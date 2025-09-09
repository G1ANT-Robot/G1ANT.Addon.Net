## How to configure Azure Application for OAuth authentication

1. In the[Azure portal](https://portal.azure.com/), open**App registrations** and click **New registration**.

2. Fill the form:

   1. **Name** of your application

   2. **Supported account type** meets your requirements

   3. ### Redirect URI

      1. From dropdown select**Public client/native (mobile & desktop)**
      2. In the textbox enter[**https://login.microsoftonline.com/common/oauth2/nativeclient**](https://login.microsoftonline.com/common/oauth2/nativeclient)

   4. Click**Register**

3. In**App registrations** select newly created application and select **Authentication**.

4. In**Advanced settings** > **Allow public client flows** > **Enable the following mobile and desktop flows:**, select**Yes**.  
   ![](https://lh6.googleusercontent.com/AFmR_xOxMl-txe6sd-4UClzcIrH8kvZak26duGG-eq712WPwJLmivs5DFKiPm_qLOyP6hI8yx8YKDZvWfwlTugs5NkB4F_yze0rQxmIrgCfo2Ba8-2iDj40P01QLeYBnZIaxins)

5. Ensure that in**Mobile and desktop applications** > **Redirect URIs** url entered during creation ([**https://login.microsoftonline.com/common/oauth2/nativeclient**](https://login.microsoftonline.com/common/oauth2/nativeclient)) is selected  
   ![](https://lh6.googleusercontent.com/EQRE_atji6Rrmz-NQvIpVKAC2_wy-lO9k3bQFKTHSO8MJUKM9PYXRi7d9wOTwl5vbyAAGdU60yZoelBRJ425xHc6nT9ohyJVIo1ee9GOj_g3c4sC_40E9-ETEQ6Z-dg5SfJJK14)

6. In the Api permission menu add following permisions and grant rights for your organization:

- email
- Mail.Read
- Mail.Read.Shared
- Mail.ReadBasic
- Mail.ReadBasic.Shared
- Mail.ReadWrite
- Mail.ReadWrite.Shared
- Mail.Send
- Mail.Send.Shared
- MailboxSettings.Read
- openid
- profile
- User.Read

7. Select**Overview**,**Application (client) ID** and **Directory (tenant) ID** values are needed to authenticate **imap/smtp** **G1ANT** commands connection
