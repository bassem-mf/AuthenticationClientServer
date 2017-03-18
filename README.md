# Authentication Client/Server
This is a simplified implementation of an OpenID Connect Provider and Relying Party using ASP.NET MVC. "Simplified" here means that all messages passed between the provider and the relying party are valid OpenID Connect messages. But not all scenarios that should be handled by an OpenID Connect provider and relying party are handled by this implementation.

This project is intended to be a reference implementation for people who want to build Web applications with external authentication functionality when they are in control of both the client and the server.

In this project, the OpenID Connect protocol is implemented from scratch using basic Controllers and Action methods (as opposed to using an OpenID Connect or oAuth library). This gives you full control over the flow and the messages passed. You probably do not want to change the flow as it is imposed by the protocol and it ensures that the authentication is done securely. But you might very well want to extend the messages to add your custom data (which is allowed by the OpenID protcol).

The following diagram shows the external authentication flow and the messages passed between the user agent (Web browser) and the Relying Party Web application (Client) and the Open ID Connect Provider Web application (Server).

![OpenID Connect sequence diagram](/SequenceDiagram.jpg?raw=true)
