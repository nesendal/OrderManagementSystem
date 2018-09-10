using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using NLog;
using OrderManagementSystem.Models;
using OrderManagementSystem.Models.DTO;

namespace OrderManagementSystem.Controllers
{
    [Authorize]
    public class CustomerOrdersController : ApiController
    {
        private OrderManagementDBEntities db = new OrderManagementDBEntities();
        Logger logger = LogManager.GetCurrentClassLogger();

        // GET: api/CustomerOrders
        [AllowAnonymous]
        public IQueryable<CustomerOrder> GetCustomerOrders()
        {
            return db.CustomerOrders;
        }

        // GET: api/CustomerOrders/5
        [ResponseType(typeof(CustomerOrder))]
        [AllowAnonymous]
        public IHttpActionResult GetCustomerOrder(int id)
        {
            CustomerOrder customerOrder = db.CustomerOrders.Find(id);
            if (customerOrder == null)
            {
                return NotFound();
            }

            return Ok(customerOrder);
        }

        // PUT: api/CustomerOrders/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutCustomerOrder(int id, CustomerOrderDTO customerOrderDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != customerOrderDTO.OrderId)
            {
                return BadRequest();
            }
            
            CustomerOrder customerOrder = db.CustomerOrders.Find(id);
            // Adres Guncelle
            if (db.CustomerAddresses.Find(customerOrderDTO.AddressId).CustomerId != customerOrder.CustomerId)
            {
                return BadRequest();
            }

            customerOrder.AddressId = customerOrderDTO.AddressId;
            db.SaveChanges();

            // Silinecekler
            var silinecekIdler = customerOrder.OrderDetails.Select(od=> od.ProductId).Except(customerOrderDTO.OrderDetails.Select(oddto => oddto.ProductId));
            var silinecekler = customerOrder.OrderDetails.Where(od => silinecekIdler.Contains(od.ProductId));
            db.OrderDetails.RemoveRange(silinecekler);

            // Adet Duzenlemeleri
            foreach (var orderDetail in customerOrder.OrderDetails)
            {
                orderDetail.Quantity = customerOrderDTO.OrderDetails.First(oddto => oddto.ProductId == orderDetail.ProductId).Quantity;
            }

            // Eklenecekler
            var eklenecekIdler = customerOrderDTO.OrderDetails.Select(oddto => oddto.ProductId).Except(customerOrder.OrderDetails.Select(od => od.ProductId));
            var eklenecekler = customerOrderDTO.OrderDetails.Where(oddto => eklenecekIdler.Contains(oddto.ProductId));
            
            for (int i = 0; i < eklenecekler.Count(); i++)
            {
                customerOrder.OrderDetails.Add(new OrderDetail() { ProductId = eklenecekler.ElementAt(i).ProductId, Quantity = eklenecekler.ElementAt(i).Quantity, OrderId = customerOrder.OrderId });
            }

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.Error(String.Format("CustomerOrder {0} Update Error: DbUpdateConcurrencyException: " + ex.Message, customerOrderDTO.OrderId));
                if (!CustomerOrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.Error(String.Format("CustomerOrder {0}  Update Error:DbUpdateConcurrencyException: " + ex.Message, customerOrderDTO.OrderId));
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/CustomerOrders
        [ResponseType(typeof(CustomerOrder))]
        public IHttpActionResult PostCustomerOrder(CustomerOrderDTO customerOrderDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            CustomerOrder customerOrder = new CustomerOrder();
            customerOrder.AddressId = customerOrderDTO.AddressId;
            customerOrder.CustomerId = customerOrderDTO.CustomerId;
            customerOrder.OrderDate = DateTime.Now;

            
            db.SaveChanges();
            logger.Info(String.Format("CustomerOrder Created: ", customerOrder.OrderId));

            for (int i = 0; i < customerOrderDTO.OrderDetails.Count; i++)
            {
                customerOrder.OrderDetails.Add(new OrderDetail() { ProductId = customerOrderDTO.OrderDetails[i].ProductId, Quantity = customerOrderDTO.OrderDetails[i].Quantity, OrderId = customerOrder.OrderId });
            }

            db.CustomerOrders.Add(customerOrder);

            try
            {
                db.SaveChanges();
                logger.Info(String.Format("OrderDetails created for CustomerOrder {0}: ", customerOrder.OrderId));

            }
            catch (DbUpdateException)
            {
                if (CustomerOrderExists(customerOrder.OrderId))
                {
                    logger.Error(String.Format("Customer Order order exists OrderId {0}: ", customerOrder.OrderId));
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = customerOrder.OrderId }, customerOrder);
        }

        // DELETE: api/CustomerOrders/5
        [ResponseType(typeof(CustomerOrder))]
        public IHttpActionResult DeleteCustomerOrder(int id)
        {
            CustomerOrder customerOrder = db.CustomerOrders.Find(id);
            if (customerOrder == null)
            {
                return NotFound();
            }

            if (customerOrder.OrderDetails.Count > 0)
            {
                db.OrderDetails.RemoveRange(customerOrder.OrderDetails);
            }

            db.CustomerOrders.Remove(customerOrder);
            db.SaveChanges();

            return Ok(customerOrder);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CustomerOrderExists(int id)
        {
            return db.CustomerOrders.Count(e => e.OrderId == id) > 0;
        }
    }
}