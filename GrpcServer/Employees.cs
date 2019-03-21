using Messages;
using System.Collections.Generic;

namespace GrpcServer
{
    public static class Employees
    {
        public static List<Employee> employees = new List<Employee>()
        {
            new Employee
            {
                Id = 1,
                BadgeNumber = 2080,
                FirstName = "Grace",
                LastName = "Decker",
                VacationAccrualRate = 2,
                VacationAccrued = 30
            },
            new Employee {
                Id= 2,
                BadgeNumber= 7538,
                FirstName= "Amity",
                LastName= "Fuller",
                VacationAccrualRate= 2.3f,
                VacationAccrued= 23.4f
            },
            new Employee {
                Id= 3,
                BadgeNumber= 5144,
                FirstName= "Keaton",
                LastName= "Willis",
                VacationAccrualRate= 3,
                VacationAccrued= 31.7f
            }
        };
    }
}
