using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SignalMonitoring.API.Hubs;
using WebAPI.Models;

namespace CRUD_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentDetailController : ControllerBase
    {
        private readonly PaymentDetailContext _context;
        private readonly IHubContext<SignalHub> _hubContext;

        public IConfiguration Configuration { get; }

        public PaymentDetailController(PaymentDetailContext context, IHubContext<SignalHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/PaymentDetail
        [HttpGet]
        public IEnumerable<PaymentDetail> GetPaymentDetails()
        {

            // method 1 using sql SqlDependency
            string constr = @"Data Source=LAPTOP-1LPU1FCC\SQLEXPRESS;Initial Catalog=PaymentDetailDB;integrated security=true";
            SqlDependency.Start(constr);
            using (var conn = new SqlConnection(constr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"SELECT [PMId]
                    ,[CardOwnerName]
                    ,[CardNumber]
                    ,[ExpirationDate]
                    ,[CVV]
                    FROM [dbo].[PaymentDetails]", conn))
                {
                    cmd.Notification = null;
                    SqlDependency dependency = new SqlDependency(cmd);
                    dependency.OnChange += Dependency_OnChange;

                    if (conn.State == System.Data.ConnectionState.Closed)
                        conn.Open();


                    var reader = cmd.ExecuteReader();
                   
                }
            }
            return _context.PaymentDetails;
        }

        // GET: api/PaymentDetail/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentDetail([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentDetail = await _context.PaymentDetails.FindAsync(id);

            if (paymentDetail == null)
            {
                return NotFound();
            }

            return Ok(paymentDetail);
        }

        // PUT: api/PaymentDetail/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaymentDetail([FromRoute] int id, [FromBody] PaymentDetail paymentDetail)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != paymentDetail.PMId)
            {
                return BadRequest();
            }

            _context.Entry(paymentDetail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                // method 2 by sending the data from the api
                await _hubContext.Clients.All.SendAsync("SignalMessageReceived", paymentDetail);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentDetailExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/PaymentDetail
        [HttpPost]
        public async Task<IActionResult> PostPaymentDetail([FromBody] PaymentDetail paymentDetail)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.PaymentDetails.Add(paymentDetail);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("SignalMessageReceived", paymentDetail);

            return CreatedAtAction("GetPaymentDetail", new { id = paymentDetail.PMId }, paymentDetail);
        }

        // DELETE: api/PaymentDetail/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentDetail([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentDetail = await _context.PaymentDetails.FindAsync(id);
            if (paymentDetail == null)
            {
                return NotFound();
            }

            _context.PaymentDetails.Remove(paymentDetail);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("SignalMessageReceived", paymentDetail);

            return Ok(paymentDetail);
        }

        private bool PaymentDetailExists(int id)
        {
            return _context.PaymentDetails.Any(e => e.PMId == id);
        }

        private void Dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                _hubContext.Clients.All.SendAsync("GetDetails", sender);
            }
        }
    }
}