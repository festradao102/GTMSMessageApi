using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using messageapi;
using messageapi.Models;
using Apache.NMS;           // queue message activeMQ with aws
using Apache.NMS.Util;
using MailKit.Net.Smtp;     // packages used to send emails in .net
using MailKit;
using MimeKit;
using System.Text.RegularExpressions;

namespace messageapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceivedMessageController : ControllerBase
    {
        private readonly GtmsDbContext _context;
        private ReceivedMessage rcvdMsg = new ReceivedMessage();

        public ReceivedMessageController(GtmsDbContext context)
        {
            _context = context;
        }

        // GET: api/ReceivedMessage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReceivedMessage>>> GetReceivedMessages()
        {
            ReadQueueMessages();

            _context.Add(rcvdMsg);
            await _context.SaveChangesAsync();  
            return await _context.ReceivedMessages.ToListAsync();
        }
    

        // GET: api/ReceivedMessage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReceivedMessage>> GetReceivedMessage(int id)
        {
            var receivedMessage = await _context.ReceivedMessages.FindAsync(id);

            if (receivedMessage == null)
            {
                return NotFound();
            }

            return receivedMessage;
        }

        // PUT: api/ReceivedMessage/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReceivedMessage(int id, ReceivedMessage receivedMessage)
        {
            if (id != receivedMessage.idMessage)
            {
                return BadRequest();
            }

            _context.Entry(receivedMessage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReceivedMessageExists(id))
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

        // POST: api/ReceivedMessage
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        #pragma warning disable 1998
        [HttpPost]
        public async Task<ActionResult<ReceivedMessage>> PostReceivedMessage(ReceivedMessage receivedMessage)
        {

            return CreatedAtAction("GetReceivedMessage", new { id = receivedMessage.idMessage }, receivedMessage);
        }

        // DELETE: api/ReceivedMessage/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ReceivedMessage>> DeleteReceivedMessage(int id)
        {
            var receivedMessage = await _context.ReceivedMessages.FindAsync(id);
            if (receivedMessage == null)
            {
                return NotFound();
            }

            _context.ReceivedMessages.Remove(receivedMessage);
            await _context.SaveChangesAsync();

            return receivedMessage;
        }

        private bool ReceivedMessageExists(int id)
        {
            return _context.ReceivedMessages.Any(e => e.idMessage == id);
        }

        public string ReadQueueMessages(){
            // definir nombre del queue, en caso de no existir activemq lo crea
            // definir el uri del endpoint de aw - aws acepta ssl no tcp asi que el string cambia en el protocolo
            // al ser ssl hay que enviar las credenciales
            // apache.nms connectionfactory provee manejo de comunicacion con el queue
            // definir el "producer" en caso de la clase que envia de mensaje 
            // definir el "receiver" en caso de ser el servicio consumiendo el queue 

            Console.WriteLine("< --- Starting GTMS queue messaging system --- >");

            string queueName = "dev_queue";   
            //string brokerUri = $"activemq:tcp://localhost:61616";  // dev broker
            
            string brokerUri = $"activemq:ssl://b-92a12260-5d2f-4a73-be53-6c78e22ef8b4-1.mq.us-east-2.amazonaws.com:61617";  // prod broker
            NMSConnectionFactory factory = new NMSConnectionFactory(brokerUri);
        
            using (IConnection connection = factory.CreateConnection("admin","adminactivemq"))  
            {
                connection.Start();
                using (Apache.NMS.ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                using (IDestination dest = session.GetQueue(queueName))
                using (IMessageConsumer consumer = session.CreateConsumer(dest))
            {
                IMessage msg = consumer.Receive();
                if (msg is ITextMessage)
                {
                    Console.WriteLine($"Adding message to queue: {queueName}"); 

                    ITextMessage txtMsg = msg as ITextMessage;
                    string body = txtMsg.Text;    
                    Console.WriteLine($"Received message: {txtMsg.Text}");
                    connection.Close();

                    sendEmail(body);
                    rcvdMsg.Description = body;
                    rcvdMsg.ReceivedDate = System.DateTime.Now;                        
                }
                else
                {
                    Console.WriteLine("Unexpected message type or message is empty: " + msg.GetType().Name);
                }
                
                return "Received message(s)";
                }
            }
        }

            public void sendEmail(String messageBody){    

            //extract email
            //esto deberia ser con xml parser, no con expr. regulares !!
            string pattern = "<Email>(.*?)</Email>";
            string input = messageBody;
            string toEmail;

            Match match = Regex.Match(input, pattern);
            if (match.Success){
                System.Console.WriteLine("El email es " + match.Groups[1].Value);
                toEmail = match.Groups[1].Value;            

                //instanciar un mimemessage
                var message = new MimeMessage();                 

                //from
                message.From.Add(new MailboxAddress("Admin torneo","devgtms@gmail.com"));

                //to
                message.To.Add(new MailboxAddress("Jugador registrado",toEmail));

                //subject
                message.Subject = "Registro Torneo";

                //body
                message.Body = new TextPart("plain"){
                    Text = "Usted ha sido registrado para el campeonato de futbol. Por favor pongase en contacto con su entrenador."
                };

                //email config
                using(var client = new SmtpClient()){
                    client.Connect("smtp.gmail.com", 587, false);   //server, port, useSSL
                    client.Authenticate("devgtms@gmail.com","#####");
                    client.Send(message);
                    client.Disconnect(true);
                }
            }  
        }

    }
}

