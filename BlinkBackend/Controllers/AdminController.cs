﻿using BlinkBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

namespace BlinkBackend.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AdminController : ApiController
    {
        readonly BlinkMovie2Entities db = new BlinkMovie2Entities();
        static int GenerateId()
        {

            long timestamp = DateTime.Now.Ticks;
            Random random = new Random();
            int randomComponent = random.Next();

            int userId = (int)(timestamp ^ randomComponent);

            return Math.Abs(userId);
        }
        [HttpPut]
        public HttpResponseMessage AcceptBalanceRequest(int id)
        {
            using (var db = new BlinkMovie2Entities())
            {
                var balanceRequest = db.BalanceRequests.FirstOrDefault(br => br.Balance_ID == id);

                if (balanceRequest == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Balance request not found.");
                }

                if (balanceRequest.Status == "Accepted")
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Balance request is already accepted.");
                }

                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == balanceRequest.Reader_ID);

                if (reader == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found.");
                }

                reader.Balance += balanceRequest.Balance;
                reader.Subscription = "Paid";

                balanceRequest.Status = "Accepted";

                db.SaveChanges();

                // Schedule the balance reset after 30 seconds
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(120)); 
                    ResetBalanceAndSubscription(id);
                });

                return Request.CreateResponse(HttpStatusCode.OK, "Balance request accepted and reader's balance updated.");
            }
        }

        private void ResetBalanceAndSubscription(int id)
        {
            using (var db = new BlinkMovie2Entities())
            {
                var balanceRequest = db.BalanceRequests.FirstOrDefault(br => br.Balance_ID == id);

                if (balanceRequest != null)
                {
                    var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == balanceRequest.Reader_ID);

                    if (reader != null)
                    {
                        reader.Balance = 0;
                        reader.Subscription = "Free";
                        db.SaveChanges();
                    }
                }
            }
        }

        [HttpPut]
        public HttpResponseMessage RejectBalanceRequest(int id) 
        {
            try 
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();
                var balanceRequest = db.BalanceRequests.FirstOrDefault(r => r.Balance_ID == id);
                if (balanceRequest != null)
                {
                    balanceRequest.Status = "Rejected";
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Balance Request Rejected");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Balance Request not found");
                }


            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage ShowAllUser() 
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var Reader = db.Reader.Select(b => new
            {
                b.Reader_ID,
                b.Subscription,
                b.Interest,
                b.Balance,
                b.UserName,
                b.Image,
                b.Email
            });

            return Request.CreateResponse(HttpStatusCode.OK, Reader);
        }
        [HttpGet]
        public HttpResponseMessage ShowAllWriters()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var Writer = db.Writer.Select(b => new
            {
                b.Writer_ID,
                b.UserName,
                b.Interest,
                b.Balance,
                b.Image
            });
            return Request.CreateResponse(HttpStatusCode.OK, Writer);
        }
        [HttpGet]
        public HttpResponseMessage ShowAllComopany()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var Writer = db.Company.Select(b => new
            {
                b.Image,
                b.Name,
                b.Company_ID,
                b.Balance,
                
                
            });
            return Request.CreateResponse(HttpStatusCode.OK, Writer);
        }

        [HttpGet]
        public HttpResponseMessage ShowAllEditors()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var Writer = db.Editor.Select(b => new
            {
                b.Editor_ID,
                b.UserName,
                b.Interest,
                b.Email,
                
            });
            return Request.CreateResponse(HttpStatusCode.OK, Writer);
        }


        [HttpGet]
        public HttpResponseMessage GetBalanceRequests()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            var balanceRequests = db.BalanceRequests.Where(br => br.Status == "Sent")
                    .Select(br => new
                    {
                        br.Balance_ID,
                        br.Balance,
                        br.RequestDate,
                        br.Status,
                        ReaderDetails = db.Reader
                            .Where(r => r.Reader_ID == br.Reader_ID)
                            .Select(r => new
                            {
                                r.UserName,
                                r.Email,
                                r.Image
                            })
                            .FirstOrDefault()
                    }).OrderBy(s => s.RequestDate)
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, balanceRequests);
            
        }

        [HttpGet]
        public HttpResponseMessage GetAdminNotificationCount()
        {
            try
            {
                using (var db = new BlinkMovie2Entities())
                {
                    db.Configuration.LazyLoadingEnabled = false;
                    db.Configuration.ProxyCreationEnabled = false;

                    var notificationCount = db.BalanceRequests
                                              .Count(br => br.adminNotifications == true);
                    var notification = db.BalanceRequests
                                         .Where(br => br.adminNotifications == true)
                                         .Select(s => new
                                         {

                                             Balance_ID = s.Balance_ID
                                         })
                                         .ToList();

                    var response = new
                    {
                        count = notificationCount,
                        Notification = notification
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            catch (Exception ex)
            {
                // Log the exception details (ex)
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPut]

        public HttpResponseMessage ResetAdminNotifications()
        {
            using (var db = new BlinkMovie2Entities())
            {
                var balanceRequests = db.BalanceRequests.Where(br => br.adminNotifications == true);

                foreach (var request in balanceRequests)
                {
                    request.adminNotifications = false;
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Admin notifications reset successfully.");
            }
        }


        [HttpDelete]
        public HttpResponseMessage DeleteUser(int id, string role)
        {
            try
            {
                switch (role.ToLower())
                {
                    case "writer":
                        var writer = db.Writer.FirstOrDefault(w => w.Writer_ID == id);
                        if (writer == null)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, "Writer not found");
                        }

                        db.Writer.Remove(writer);
                        db.SaveChanges();

                        return Request.CreateResponse(HttpStatusCode.OK, "Writer successfully deleted");

                    case "reader":
                        var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == id);
                        if (reader == null)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found");
                        }

                        db.Reader.Remove(reader);
                        db.SaveChanges();

                        return Request.CreateResponse(HttpStatusCode.OK, "Reader successfully deleted");

                    case "editor":
                        var editor = db.Editor.FirstOrDefault(e => e.Editor_ID == id);
                        if (editor == null)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, "Editor not found");
                        }

                        db.Editor.Remove(editor);
                        db.SaveChanges();

                        return Request.CreateResponse(HttpStatusCode.OK, "Editor successfully deleted");

                    default:
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid role specified");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddEditor(Editor editorForm)
        {
            try
            {
                
                var newEditor = new Editor
                {
                    Email = editorForm.Email,
                    Password = editorForm.Password,
                    UserName = editorForm.UserName
                };

                db.Editor.Add(newEditor);
                db.SaveChanges();

                
                var newEditorUser = new Users
                {
                    Editor_ID = newEditor.Editor_ID,
                    Email = editorForm.Email,
                    Password = editorForm.Password,
                    Role = "editor", 
                };

                db.Users.Add(newEditorUser);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.Created, "Editor successfully added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage Login(string email, string username)
        {

            try
            {
                var admin = db.Admin
                              .Where(a => a.Email == email && a.UserName == username)
                              .FirstOrDefault();

                if (admin != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, admin);
                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Admin not found.");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddAdvertisement(int CompanyID,String List1) 
        {
            try { 
                string[] add=List1.Split(',');
                foreach(string item in add)
                {
                    var addAdver = new Advertismet
                    {
                        Ad_ID = GenerateId(),
                        Company_ID = CompanyID,
                        Url = item,
                        count = 0,
                        };
                    db.Advertismet.Add(addAdver);
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK,"Add Advertisement");
                
            }
            catch (Exception ex) {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetRandomAdvertisement()
        {

            try { 
                var randomAdver=db.Advertismet.OrderBy(r=>Guid.NewGuid()).FirstOrDefault();
                
                db.SaveChanges();

                var responce = new
                {
                    randomAdver = randomAdver,
                    Clips_id = GenerateId(),
                    Starttime = 0.0,
                    EndTime=5.0,
                };
                randomAdver.count = randomAdver.count + 1;

                return Request.CreateResponse(HttpStatusCode.OK, responce);

            }catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetCompanyData() {

            try {
                var randomAdver = db.Advertismet.Where(c => c.count >= 1).Select(s => new

                {
                    s.Company_ID,
                    s.count,
                    CompanyData=db.Company.Where(s1=>s1.Company_ID==s.Company_ID).Select(data=>new

                    {
                        data.Company_ID,
                        data.Name,
                        data.Email,
                        s.count
                    })
                }).ToList();
                db.SaveChanges();
                var Response = new
                {
                    randomAdver = randomAdver,
                    ClipsId = GenerateId(),

                };
                return Request.CreateResponse(HttpStatusCode.OK, Response);
            }catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }

    }
}
