using Grpc.Core;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcServer
{
    public class EmployeeService : Messages.EmployeeService.EmployeeServiceBase
    {
        /// <summary>
        /// Unary message: one request <-> one response
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<EmployeeResponse> GetByBadgeNumber(GetByBadgeNumberRequest request, ServerCallContext context)
        {
            var md = context.RequestHeaders;
            Console.WriteLine("Requested employee with badgenumber: " + request.BadgeNumber);
            Console.WriteLine("Printing metadata...");
            foreach (var entry in md)
            {
                Console.WriteLine($"{entry.Key}:{entry.Value}");
            }

            var emp = Employees.employees.Where(x => x.BadgeNumber == request.BadgeNumber).FirstOrDefault();

            if (emp == null)
                throw new Exception("Employee not found..");

            return new EmployeeResponse
            {
                Employee = emp
            };
        }

        /// <summary>
        /// Server streaming
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task GetAll(GetAllRequest request, IServerStreamWriter<EmployeeResponse> responseStream, ServerCallContext context)
        {
            Console.WriteLine("GetAll is invoked. Sending all employees as a stream");
            foreach (var item in Employees.employees)
            {
                await responseStream.WriteAsync(new EmployeeResponse { Employee = item });
            }

            Console.WriteLine("Finished GetAll method.");
        }


        /// <summary>
        /// Client Streaming
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<AddPhotoResponse> AddPhoto(IAsyncStreamReader<AddPhotoRequest> requestStream, ServerCallContext context)
        {
            var md = context.RequestHeaders;
            var badgeNumberStr = md.Where(x => x.Key == "badgenumber").Select(x => x.Value).FirstOrDefault();
            if (string.IsNullOrEmpty(badgeNumberStr))
                throw new Exception("Badge number is not provided");

            var badgeNumber = int.Parse(badgeNumberStr);
            Console.WriteLine($"Uploading photo for badge number: {badgeNumber}");

            var data = new List<byte>();
            while (await requestStream.MoveNext())
            {
                Console.WriteLine($"Received: {requestStream.Current.Data.Length} bytes");
                data.AddRange(requestStream.Current.Data);
            }

            Console.WriteLine("Received file with : " + data.Count + "bytes");
            return new AddPhotoResponse
            {
                IsOk = true
            };
        }


        /// <summary>
        /// BiDirectional streaming
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task SaveAll(IAsyncStreamReader<EmployeeRequest> requestStream, IServerStreamWriter<EmployeeResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var employee = requestStream.Current.Employee;
                lock (this)
                {
                    Employees.employees.Add(employee);
                }

                await responseStream.WriteAsync(new EmployeeResponse
                {
                    Employee = employee
                });
            }
        }
    }
}
