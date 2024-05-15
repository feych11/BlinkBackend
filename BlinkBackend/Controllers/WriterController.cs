using BlinkBackend.Classes;
using BlinkBackend.Models;
using Newtonsoft.Json.Linq;
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
    public class WriterController : ApiController
    {

        static int GenerateId()
        {

            long timestamp = DateTime.Now.Ticks;
            Random random = new Random();
            int randomComponent = random.Next();

            int userId = (int)(timestamp ^ randomComponent);

            return Math.Abs(userId);
        }
        [HttpPost]
        public HttpResponseMessage RejectProposal(int SentProposals_ID)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();
                var Proposal = db.SentProposals.FirstOrDefault(r => r.SentProposal_ID == SentProposals_ID);

                if (Proposal != null)
                {
                    Proposal.Status = "Rejected";
                    Proposal.Editor_Notification = true;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Proposal Rejected");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Proposal not found");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage ViewRewriteProject()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var projects = db.SentProject
                .Where(s => s.Status == "Rewrite")
                .Select(s => new
                {
                    s.SentProject_ID,
                    s.Movie_ID,
                    s.SentProposal_ID,
                    s.Editor_ID,
                    s.Writer_ID,
                    SentProposalData = db.SentProposals
                        .Where(sp => sp.SentProposal_ID == s.SentProposal_ID)
                        .Select(sp => new
                        {
                            sp.SentProposal_ID,
                            sp.Movie_Name,
                            sp.Image,
                            sp.Genre,
                            sp.Type,
                            sp.Director
                        })
                        .FirstOrDefault()
                })
                .ToList();

            var responseContent = new
            {
                Project = projects
            };

            return Request.CreateResponse(HttpStatusCode.OK, responseContent);
        }

        [HttpPost]
        public HttpResponseMessage AcceptProposal(int SentProposals_ID)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();
                var acceptProposal = db.SentProposals.FirstOrDefault(r => r.SentProposal_ID == SentProposals_ID);

                if (acceptProposal != null)
                {
                    acceptProposal.Status = "Accepted";
                    acceptProposal.Editor_Notification = true;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Proposal Accepted");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Proposal not found");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetRewriteData(int Writer_ID)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                db.Configuration.LazyLoadingEnabled = false;


                var sentProjects = db.SentProject
                    .Where(sp => sp.Status == "Rewrite" && sp.Writer_ID == Writer_ID)
                    .ToList();

                var sProIds = sentProjects.Select(sp => sp.Movie_ID).ToList();

                var summaries = db.Summary
                    .Where(s => sProIds.Any(id => id == s.Movie_ID))
                    .Select(s => new
                    {
                        s.Summary_ID,
                        s.Movie_ID,
                        s.Writer_ID,
                        s.Summary1,

                    })
                    .ToList();

                var clips = db.Clips
                    .Where(s => sProIds.Any(id => id == s.Sent_ID))
                     .Select(s => new
                     {
                         s.Clips_ID,
                         s.Movie_ID,
                         s.Writer_ID,
                         s.Url,
                         s.Start_time,
                         s.End_time

                     })
                    .ToList();

                object result = new
                {
                    SentProjects = sentProjects,
                    Summaries = summaries,
                    Clips = clips
                };
                var responseData = new
                {
                    SentProjects = sentProjects,
                    Summaries = summaries,
                    Clips = clips
                };




                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage ShowProposals(int Writer_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var proposals = db.SentProposals
                    .Where(a => a.Status == "Sent" && a.Writer_ID == Writer_ID)
                    .OrderByDescending(p => p.Sent_at) // Order by proposal ID (assuming higher ID means newer proposal)
                    .Select(p => new
                    {
                        ID = p.SentProposal_ID,
                        Movie_Name = p.Movie_Name,
                        Write_ID = db.Writer.Where(m => m.Writer_ID == p.Writer_ID).Select(m => m.UserName).FirstOrDefault(),
                        Director = p.Director,
                        Image = p.Image,
                        Amount = p.Balance,
                        Episode = p.Episode,
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, proposals);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage HistorySentProject(int Writer_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities(); // Assuming BlinkMovie4 is your DbContext name
            try
            {
                var projects = (from sp in db.SentProject
                                join spro in db.SentProposals on sp.SentProposal_ID equals spro.SentProposal_ID
                                where sp.Status == "Sent" && sp.Writer_ID == Writer_ID
                                orderby sp.Send_at descending
                                select new
                                {
                                    SentProject_ID = sp.SentProject_ID,
                                    Status = sp.Status,
                                    Send_at = sp.Send_at,
                                    MovieName = spro.Movie_Name,
                                    Image = spro.Image
                                }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, projects);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        public HttpResponseMessage UpdateRewriteSummary(int SentProject_ID, string updatedSummary)
        {
            try
            {
                // Initialize your Entity Framework context
                using (var db = new BlinkMovie2Entities())
                {
                    // Find the Summary entry by SentProject_ID
                    var summary = db.Summary.FirstOrDefault(s => s.Sent_ID == SentProject_ID);

                    // Check if the Summary exists
                    if (summary == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Summary not found");
                    }

                    // Update the summary text
                    summary.Summary1 = updatedSummary;

                    // Update the Send_at in SentProject
                    var sentProject = db.SentProject.FirstOrDefault(s => s.SentProject_ID == SentProject_ID);
                    if (sentProject == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "SentProject not found");
                    }
                    sentProject.Send_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // Update the status to "Sent"
                    sentProject.Status = "Sent";

                    // Clear the editor's comment
                    sentProject.EditorComment = null;
                    sentProject.Editor_Notification = true;

                    // Save changes to the database
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Summary updated and SentProject status updated to Sent");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        public HttpResponseMessage SentProject(SentProjects spro)
        {

            BlinkMovie2Entities db = new BlinkMovie2Entities();
            DateTime currentDate = DateTime.Now;

            var proposal = db.SentProposals.Where(s => s.SentProposal_ID == spro.SentProposal_ID).FirstOrDefault();
            try
            {
                proposal.Status = "Received";
                db.SaveChanges();

                var project = new SentProject()
                {
                    SentProject_ID = GenerateId(),
                    Movie_ID = proposal.Movie_ID,
                    SentProposal_ID = spro.SentProposal_ID,
                    Editor_ID = proposal.Editor_ID,
                    Writer_ID = spro.Writer_ID,
                    Editor_Notification = true,

                    Send_at = currentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "Sent"
                };
                db.SentProject.Add(project);
                db.SaveChanges();

                var summary = new Summary()
                {
                    Summary_ID = GenerateId(),
                    Sent_ID = project.SentProject_ID,
                    Movie_ID = spro.Movie_ID,
                    Writer_ID = spro.Writer_ID,
                    Summary1 = spro.Summary,
                    Episode=proposal.Episode,
                    

                };

                db.Summary.Add(summary);
                db.SaveChanges();
                if (spro.Type == "Movie")
                {
                    foreach (var clip in spro.Clips)
                    {
                        var newClip = new Clips()
                        {
                            //Clips_ID = GenerateId(),
                            Sent_ID = project.SentProject_ID,
                            Writer_ID = proposal.Writer_ID,
                            Movie_ID = proposal.Movie_ID,
                            Url = clip.Url,
                            UploadDate=currentDate,
                            Title = clip.Title,
                            isCompoundClip = clip.isCompoundClip,
                            Start_time = clip.Start_Time.ToString(),
                            End_time = clip.End_Time.ToString(),
                        };

                        db.Clips.Add(newClip);
                        db.SaveChanges();
                    }
                }
                else
                {
                    foreach (var clip in spro.Clips)
                    {
                        var newDramasClip = new DramasClips()
                        {
                            //DramasClip_ID = GenerateId(),
                            Sent_ID = project.SentProject_ID,
                            Writer_ID = proposal.Writer_ID,
                            Movie_ID = proposal.Movie_ID,
                            Url = clip.Url,
                            Title = clip.Title,
                            isCompoundClip = clip.isCompoundClip,
                            Start_time = clip.Start_Time.ToString(),
                            End_time = clip.End_Time.ToString(),
                            Episode =proposal.Episode
                        };

                        db.DramasClips.Add(newDramasClip);
                        db.SaveChanges();
                    }
                }


                db.SaveChanges();

                // return Request.CreateResponse(HttpStatusCode.OK, clist);
                return Request.CreateResponse(HttpStatusCode.OK, "Project Sent");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(ex);
            }

        }
        [HttpGet]
        public HttpResponseMessage GetWriterSentProjects(int writerId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();

                var sentProjects = (from sp in db.SentProject
                                    where sp.Writer_ID == writerId
                                    join m in db.Movie on sp.Movie_ID equals m.Movie_ID
                                    join w in db.Writer on sp.Writer_ID equals w.Writer_ID
                                    join s in db.SentProposals on sp.SentProposal_ID equals s.SentProposal_ID
                                    select new
                                    {
                                        Writer_ID = sp.Writer_ID,
                                        SentProject_ID = sp.SentProject_ID,
                                        Genre = m.Category,
                                        Type = m.Type,
                                        Writer_Name = w.UserName,
                                        Status = sp.Status,
                                        Movie_Name = m.Name,
                                        Image = m.Image,
                                        Episode = s.Episode,
                                        Amount = s.Balance,
                                        Director = m.Director
                                    }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, sentProjects);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




        [HttpGet]
        public HttpResponseMessage ShowWriterRating(int Writer_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var rating = db.Writer.Where(s => s.Writer_ID == Writer_ID).FirstOrDefault();


                return Request.CreateResponse(HttpStatusCode.OK, rating.Rating);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }




        [HttpGet]
        public HttpResponseMessage AcceptedProposals(int Writer_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var proposals = db.SentProposals.Where(s => s.Writer_ID == Writer_ID && s.Status == "Accepted")
                    .Select(p => new
                    {
                        ID = p.SentProposal_ID,
                        Movie_Name = p.Movie_Name,
                        Write_ID = db.Writer.Where(m => m.Writer_ID == p.Writer_ID).Select(m => m.UserName).FirstOrDefault(),
                        Director = p.Director,
                        Image = p.Image,
                        Editor_ID = p.Editor_ID,
                        Movie_ID = p.Movie_ID,
                        Type = p.Type,
                        Episode = p.Episode
                        // genre=p.Genre,
                        //DueDate=p.DueDate
                    }).ToList();


                return Request.CreateResponse(HttpStatusCode.OK, proposals);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        //public HttpResponseMessage AcceptedProposals(int Writer_ID)
        //{
        //    BlinkMovieEntities db = new BlinkMovieEntities();
        //    try
        //    {
        //        var proposals = db.SentProposals
        //            .Where(s => s.Writer_ID == Writer_ID && s.Status == "Accepted")
        //            .Select(s => new
        //            {
        //                s.SentProposal_ID,
        //                s.Editor_ID,
        //                s.Writer_ID,
        //                s.Movie_ID,
        //                s.Image,
        //                s.Genre,
        //                s.Director,
        //                s.Status,
        //                s.DueDate
        //            })
        //            .ToList();

        //        return Request.CreateResponse(HttpStatusCode.OK, proposals);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        [HttpGet]
        public HttpResponseMessage GetSpecificProposal(int SentProposals_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;

            // Fetch proposal
            var proposal = db.SentProposals.FirstOrDefault(s => s.SentProposal_ID == SentProposals_ID);

            if (proposal != null)
            {
                // Fetch editor details using Editor_ID
                var editor = db.Editor.FirstOrDefault(e => e.Editor_ID == proposal.Editor_ID);

                // Fetch movie details using Movie_ID
                var movie = db.Movie.FirstOrDefault(m => m.Movie_ID == proposal.Movie_ID);

                if (editor != null && movie != null)
                {
                    // Create an anonymous object with the required information
                    var result = new
                    {
                        Proposal = proposal,
                        Editor = new
                        {
                            UserName = editor.UserName,
                            Email = editor.Email
                        },
                        MovieDescription = movie.Description
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Editor or Movie not found");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }
        }


        [HttpGet]
        public HttpResponseMessage GetWriterAccordingToGenre(string movieGenre)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                // Split movie categories from the parameter
                if (movieGenre != null)
                {
                    string[] categories = movieGenre.Split(',');

                    // Query writers whose interests match at least one of the provided movie categories
                    if (categories != null)
                    {
                        var writers = db.Writer.ToList() // Load writers into memory
    .Where(writer =>
        categories.Any(category =>
            writer.Interest?.Split(',')?.Contains(category.Trim()) ?? false
        )
    )
    .ToList();
                        // If no writers found, return a not found response
                        if (!writers.Any())
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, "No writers found with matching interests.");
                        }

                        // Create a response containing ratings of all matched writers
                        var writerC = writers.Select(writer => new
                        {
                            WriterID = writer.Writer_ID,
                            UserName = writer.UserName,
                        }).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, writerC);
                    }

                    else
                    {
                        return null;
                    }
                }

                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Return internal server error if an exception occurs
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]

        public HttpResponseMessage GetSpecificWriter(int Writer_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;

            var writer = db.Writer.FirstOrDefault(w => w.Writer_ID == Writer_ID);
            if (writer != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, writer);
            }

            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }

        }

        // NEW FUNCTION

        [HttpGet]
        public HttpResponseMessage getAllWrite()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;
            var writer = db.Writer.Select(w => new
            {
                Id = w.Writer_ID,
                Name = w.UserName
            }).ToList();
            if (writer != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, writer);
            }

            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateWriterNotifications(int writerId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProposals = db.SentProposals.Where(sp => sp.Writer_ID == writerId).ToList();

                if (sentProposals.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProposals found for the specified writer");
                }

                foreach (var sentProposal in sentProposals)
                {
                    sentProposal.Writer_Notification = false;
                }


                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Writer notifications updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetSentProposalsIdsWithWriterNotification(int writerId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProposalIds = db.SentProposals
                                        .Where(sp => sp.Writer_ID == writerId && sp.Writer_Notification == true)
                                        .Select(sp => new
                                        {
                                            sp.SentProposal_ID,
                                            sp.Status
                                        })
                                        .ToList();

                if (sentProposalIds.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProposals found with Writer_Notification true for the specified writer");
                }


                int totalCount = sentProposalIds.Count;


                var responseData = new
                {
                    SentProposalIds = sentProposalIds,
                    TotalCount = totalCount
                };

                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateAllWriterNotificationstoFalse(int writerId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProposals = db.SentProposals.Where(sp => sp.Writer_ID == writerId).ToList();

                if (sentProposals.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProposals found for the specified writer");
                }


                foreach (var sentProposal in sentProposals)
                {
                    sentProposal.Writer_Notification = false;
                }


                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "All Writer notifications updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetWriterNotificationsSentProject(int writerId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var writerNotifications = db.SentProject
                                            .Where(sp => sp.Writer_Notification == true && sp.Writer_ID == writerId)
                                            .Select(sp => new
                                            {
                                                SentProject_ID = sp.SentProject_ID,
                                                Status = sp.Status
                                            })
                                            .ToList();

                if (writerNotifications.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No writer notifications found for the specified writer");
                }


                int totalCount = writerNotifications.Count;


                var responseData = new
                {
                    WriterNotifications = writerNotifications,
                    TotalCount = totalCount
                };

                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateAllWriterNotificationsToFalseSentProject(int writerId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProjects = db.SentProject.Where(sp => sp.Writer_ID == writerId).ToList();

                if (sentProjects.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProject records found for the specified writer");
                }

                foreach (var sentProject in sentProjects)
                {
                    sentProject.Writer_Notification = false;
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "All Writer notifications updated to false for the specified writer");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        //new function
        [HttpPost]
        public HttpResponseMessage SaveClip([FromBody] JObject requestData)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                Clips clips = requestData["clip"].ToObject<Clips>();
                var c = new Clips
                {
                    Clips_ID = GenerateId(),
                    Start_time = clips.Start_time,
                    End_time = clips.End_time,
                    Url = clips.Url,
                    isCompoundClip = clips.isCompoundClip
                };
                db.Clips.Add(c);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, c);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, ex.Message);
            }

        }


        [HttpGet]
        public HttpResponseMessage HistoryAcceptedProject(int Writer_ID)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var projects = db.SentProject
                    .Where(s => s.Status == "Accepted" && s.Writer_ID == Writer_ID)
                    .OrderByDescending(s => s.Send_at)
                    .Select(s => new
                    {
                        s.Movie_ID,
                        s.Writer_ID,
                        s.SentProject_ID,
                        s.SentProposal_ID,
                        ProposalData = db.SentProposals
                            .Where(sp => sp.SentProposal_ID == s.SentProposal_ID)
                            .Select(sp => new
                            {
                                sp.Movie_Name,
                                sp.Image,
                                sp.Director,
                                sp.Type
                            })
                            .FirstOrDefault(),
                        s.Status
                    })
                    .ToList();

                var responseContent = new
                {
                    Project = projects
                };

                return Request.CreateResponse(HttpStatusCode.OK, responseContent);
            }
        }



    }


}
