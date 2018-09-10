using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OrderManagementSystem.Models.DTO
{
    public class CustomerOrderDTO
    {
        public CustomerOrderDTO()
        {
            this.OrderDetails = new List<OrderDetailDTO>();
        }

        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int AddressId { get; set; }

        public List<OrderDetailDTO> OrderDetails { get; set; }
    }

    public class OrderDetailDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}