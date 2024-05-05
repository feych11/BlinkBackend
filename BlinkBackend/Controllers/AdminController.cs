using BlinkBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace BlinkBackend.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AdminController : ApiController
    {
        readonly BlinkMovie2Entities db = new BlinkMovie2Entities();

        [HttpPut]
        public HttpResponseMessage AcceptBalanceRequest(int Reader_ID)
        {
            
                var balanc = db.BalanceRequests.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                //var reader =db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);    

                if (balanc != null)
                {
                  //  reader.Balance += balanc.Balance;
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Balance updated successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
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
    }
}
