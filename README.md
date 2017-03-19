# Authentication Client/Server
This is a simplified implementation of an OpenID Connect Provider and Relying Party using ASP.NET MVC. "Simplified" here means that all messages passed between the provider and the relying party are valid OpenID Connect messages. But not all scenarios that should be handled by an OpenID Connect provider and relying party are handled by this implementation.

This project is intended to be a reference implementation for people who want to build Web applications with external authentication functionality when they are in control of both the client and the server.

In this project, the OpenID Connect protocol is implemented from scratch using basic Controllers and Action methods (as opposed to using an OpenID Connect or oAuth library). This gives you full control over the flow and the messages passed. You probably do not want to change the flow as it is imposed by the protocol and it ensures that the authentication is done securely. But you might very well want to extend the messages to add your custom data (which is allowed by the OpenID protcol).

The following diagram shows the external authentication flow and the messages passed between the user agent (Client Browser) and the authentication client Web application (Relying Party) and the authentication server Web application (Identity Provider).

![OpenID Connect sequence diagram](/SequenceDiagram.jpg?raw=true)

## Setting Up Your Local Environment to Run the Web Applications

1. Add the following entries to your "hosts" file:
   
   ```
   127.0.0.1	provider.localhost
   127.0.0.1	relyingparty.localhost
   ```
   to point the hostnames used by the project ("provider.localhost" and "relyingparty.localhost") to the loopback IP address "127.0.0.1". On my computer, the "hosts" file is located at
   ```
   C:\Windows\System32\drivers\etc\hosts
   ```

2. Run Visual Studio **as an administrator** and open "AuthenticationClientServer.sln". The two website projects will fail to load. But a hidden ".vs" folder will be created under the solution folder which is what we need.

3. Open `<solution folder>\.vs\config\applicationhost.config` for editing and add the following XML elements under the `<sites>` element. Remember to replace the physicalPath place holders below.
   ```xml
   <site name="Provider" id="2">
       <application path="/" applicationPool="Clr4IntegratedAppPool">
           <virtualDirectory path="/" physicalPath="Path to your Provider project folder goes here" />
       </application>
       <bindings>
           <binding protocol="http" bindingInformation="*:50343:provider.localhost" />
       </bindings>
   </site>
   <site name="RelyingParty" id="3">
       <application path="/" applicationPool="Clr4IntegratedAppPool">
           <virtualDirectory path="/" physicalPath="Path to your RelyingParty project folder goes here" />
       </application>
       <bindings>
           <binding protocol="http" bindingInformation="*:57339:relyingparty.localhost" />
       </bindings>
   </site>
   ```

4. Right-Click each of the website projects in the Solution Explorer and select "Reload Project".

5. In the Solution Explorer, right-click Provider > Debug > Start new instance.<br />
   The website will open in a browser window. Click "Register" on the top-right and fill the form to create a new user account.<br />
   Click "Log off" from the top-right.

6. Go back to Visual Studio, right-click the solution item in the Solution Explorer and select "Set StartUp Projects..."<br />
   Select "Multiple startup projects".<br />
   Next to "Provider" select "Start" and next to "RelyingParty" select "Start".<br />
   Click OK.

7. Right-Click the "Provider" project in the Solution Explorer and select "Properties".<br />
   From the left tabs select "Web".<br />
   Set the "Start Action" to "Don't open a page. Wait for a request from an external application." and save.

8. Hit F5 and login to the RelyingParty using your Provider account.


#### References:
* [OpenID Connect specification](http://openid.net/specs/openid-connect-core-1_0.html)
* [OAuth 2.0 specification](https://tools.ietf.org/html/rfc6749)
