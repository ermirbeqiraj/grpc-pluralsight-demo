using Grpc.Core;
using Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GrpcClient
{
    class Program
    {
        static SslCredentials GetSslCredentials()
        {
            var CERT_PATH = Path.Combine(Environment.CurrentDirectory, "Certs");
            var cacert = File.ReadAllText(Path.Combine(CERT_PATH, "ca.crt"));
            var cert = File.ReadAllText(Path.Combine(CERT_PATH, "client.crt"));
            var key = File.ReadAllText(Path.Combine(CERT_PATH, "client.key"));

            var keyPair = new KeyCertificatePair(cert, key);
            var Creds = new SslCredentials(cacert, keyPair);
            return Creds;
        }

        static void Main(string[] args)
        {
            const int Port = 50051;
            const string Host = "127.0.0.1";

            try
            {
                var creds = GetSslCredentials();

                var PcName = Environment.MachineName;
                var channelOptions = new List<ChannelOption>
                {
                    // this will get rid of nds failure error
                    new ChannelOption(ChannelOptions.SslTargetNameOverride, PcName)
                };
                var channel = new Channel(Host, Port, creds, channelOptions);
                var client = new EmployeeService.EmployeeServiceClient(channel);

                do
                {
                    Console.WriteLine("\n-----------------OPTIONS------------------");
                    Console.WriteLine("1. GetByBadgeNumber \t(UO)\n" +
                        "2. GetAll \t(SS)\n" +
                        "3. AddPhoto\t(CS)\n" +
                        "4. SaveAll \t(DS)\n" +
                        "- Type any other number to exit");
                    var input = Console.ReadLine().Trim();
                    switch (input)
                    {
                        case "1":
                            GetByBadgeNumber(client).Wait();
                            break;
                        case "2":
                            GetAll(client).Wait();
                            break;
                        case "3":
                            AddPhoto(client).Wait();
                            break;
                        case "4":
                            SaveAll(client).Wait();
                            break;
                        default:
                            return;
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // unary operation
        static async Task GetByBadgeNumber(EmployeeService.EmployeeServiceClient client)
        {
            Console.WriteLine($"GetByBadgeNumber");

            var md = new Metadata
            {
                new Metadata.Entry("username", "ermir")
            };

            var result = await client.GetByBadgeNumberAsync(new GetByBadgeNumberRequest
            {
                BadgeNumber = 2080
            }, headers: md);
            Console.WriteLine(result);
        }

        // server stream
        static async Task GetAll(EmployeeService.EmployeeServiceClient client)
        {
            Console.WriteLine($"GetAll - Server streaming sample");

            using (var call = client.GetAll(new GetAllRequest()))
            {
                var responserStream = call.ResponseStream;
                while (await responserStream.MoveNext())
                {
                    Console.WriteLine(responserStream.Current.Employee);
                }
            }
        }

        //client stream
        static async Task AddPhoto(EmployeeService.EmployeeServiceClient client)
        {
            var md = new Metadata
            {
                new Metadata.Entry("badgenumber", "2080")
            };
            //credits: Photo by Daniel Norris on Unsplash
            var imagePath = Path.Combine(Environment.CurrentDirectory, "Data", "daniel-norris-405371-unsplash.jpg");
            using (var fs = File.OpenRead(imagePath))
            using (var call = client.AddPhoto(headers: md))
            {
                var stream = call.RequestStream;
                while (true)
                {
                    var buffer = new byte[64 * 1024];// 64kb
                    var numRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                    if (numRead == 0)
                        break;

                    if (numRead < buffer.Length)
                        Array.Resize(ref buffer, numRead);

                    await stream.WriteAsync(new AddPhotoRequest
                    {
                        Data = Google.Protobuf.ByteString.CopyFrom(buffer)
                    });
                }

                await stream.CompleteAsync();
                var res = await call.ResponseAsync;
                Console.WriteLine($"Finished uploading, IsOk: {res.IsOk}");
            }
        }

        //duplex stream
        static async Task SaveAll(EmployeeService.EmployeeServiceClient client)
        {
            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = 123,
                    BadgeNumber = 1,
                    FirstName = "Ermir",
                    LastName = "Beqiraj",
                    VacationAccrualRate = 1.7f,
                    VacationAccrued = 10
                }
            };

            using (var call = client.SaveAll())
            {
                var requestStream = call.RequestStream;
                var responseStream = call.ResponseStream;

                // !important note: as soon as first request is made, we don't know how the server will response,
                // it may respond directly after first request, or it might collect responses
                // and respond after all requests have been finished,
                // so: we should handle responses async from requests
                var responseTask = Task.Run(async () =>
                {
                    while (await responseStream.MoveNext())
                    {
                        Console.WriteLine($"Saved : {responseStream.Current.Employee}");
                    }
                });


                foreach (var item in employees)
                {
                    await requestStream.WriteAsync(new EmployeeRequest { Employee = item });
                }
                // let server know we are done sending requests
                await requestStream.CompleteAsync();
                // wait after letting server know we finished requests
                await responseTask;
            }
        }
    }
}
