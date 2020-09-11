FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
COPY bin/Release/netcoreapp3.1/publish/ TCPResponderTest/
WORKDIR /TCPResponderTest
EXPOSE 6060
ENTRYPOINT ["dotnet", "TCPResponderTest.dll"]
