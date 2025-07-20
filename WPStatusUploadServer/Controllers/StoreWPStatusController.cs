using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;


namespace geothermieUploadServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StoreWPStatusController : ControllerBase
    {
        private readonly IConfiguration _config;
        public StoreWPStatusController(IConfiguration config)
        {
            _config = config;
        }
        [HttpGet("")]
        public async Task<IActionResult> StoreWPStatus(string currentState)
        {
            //Status = HEIZEN oder KÜHLEN
            using var connection = new SqlConnection(_config.GetConnectionString("wpstatusConnection"));

            var p = new DynamicParameters();
            p.Add("currentState", currentState);
            p.Add("haschanged", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("statid", dbType: DbType.Int32 , direction: ParameterDirection.Output);
            //ToDo: Rückgabewert aus SP, falls Änderung zu vorherigem Stand, der älter als 4 Stunden sein sollte
            connection.Query<int>("spStoreWPStatus", p, commandType: CommandType.StoredProcedure);
            //ToDo: Bei Änderung Mails versenden
            Serilog.Log.Information("WP-Status gespeichert: " + currentState + "<br/>");
            bool hasChanged = p.Get<bool>("haschanged");
            int statid = p.Get<int>("statid");
            //jetzt noch prüfen, ob Änderung vorliegt und eMails versendet werden müssen
            if (hasChanged )
            {
                Serilog.Log.Information("WP-Status has changed.");
                clsCommunication communication = new clsCommunication();
                string mailBody = "Bitte nun bei allen Heizkreisverteilern die mit dem Eingang CO verbundenen Schalter auf 0 (=HEIZEN) stellen.";
                if (currentState == "KÜHLEN")
                {
                    mailBody = "Bitte nun bei allen Heizkreisverteilern die mit dem Eingang CO verbundenen Schalter auf 1 (=KÜHLEN) stellen.";
                }
                mailBody += "\n\nhttps://waermepumpe.halbinsulaner.de/";
                string subject = "Status der Wärmepumpe hat sich geändert auf " + currentState;
                //Empfängerliste: Exportieren aus paketraum-DB und in appsettings.json eintragen
                //SELECT STRING_AGG(iif(sms LIKE '%@%', sms, email),',') FROM empfänger
                Serilog.Log.Information("Mail versenden: " + subject +" - "+mailBody);
                communication.sendMail (subject ,mailBody, "mail@planserver.pro", _config["NotificationEMails"],_config);
                var ps = new DynamicParameters();
                ps.Add("statid", p.Get<int>("statid"));
                connection.Execute("spWPmailHasBeenSent", ps,commandType: CommandType.StoredProcedure);
                Serilog.Log.Information("Mail versendet und Versendung in DB eingetragen.");
            }

            // File für Website aktualisieren
            var json = connection.ExecuteScalar<string>("select dbo.fnWPstatusJSON()");
            Serilog.Log.Information("Neues JSON: "+json);
            try
            {
                System.IO.File.Delete(_config["WPjsonDestFile"]);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("Fehler beim Löschen der Datei: " + ex.Message);
            }   

            System.IO.File.WriteAllText(_config["WPjsonDestFile"], json); 
            //
            return Ok(currentState);
        }


    }
}

