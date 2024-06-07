using BlinkBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Web;
using Newtonsoft.Json;
using System.Web.Http;
using System.IO;
using System.Web.Http.Cors;
using Microsoft.AspNetCore.Mvc;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using HttpPutAttribute = System.Web.Http.HttpPutAttribute;
using HttpDeleteAttribute = System.Web.Http.HttpDeleteAttribute;
using Microsoft.AspNetCore.Hosting.Server;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;
using BlinkBackend.Classes;

namespace BlinkBackend.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EditorController : ApiController
    {


        static int GenerateId()
        {

            long timestamp = DateTime.Now.Ticks;
            Random random = new Random();
            int randomComponent = random.Next();

            int userId = (int)(timestamp ^ randomComponent);

            return Math.Abs(userId);
        }


        [HttpPut]
        public HttpResponseMessage UpdateInterests(int Editor_ID, string newInterests)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var editor = db.Editor.FirstOrDefault(r => r.Editor_ID == Editor_ID);

                if (editor != null)
                {
                    editor.Interest = newInterests;
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Interests updated successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Editor not found");
                }
            }
        }

        [HttpGet]
        public HttpResponseMessage GetAllMovies()
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var movies = db.Movie.Where(m => m.Type == "Movie").ToList();

                if (movies.Any())
                {
                    string movieJson = JsonConvert.SerializeObject(movies, jsonSettings);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(movieJson, Encoding.UTF8, "application/json");
                    return response;
                    //return Request.CreateResponse(HttpStatusCode.OK, movies);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No movies found");
                }
            }
        }

       


        [HttpGet]
        public HttpResponseMessage GetAllMoviesName()
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var movies = db.Movie.Select(w => new
                {
                    Id = w.Movie_ID,
                    Name = w.Name,
                }).ToList();

                if (movies.Any())
                {
                    string movieJson = JsonConvert.SerializeObject(movies, jsonSettings);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(movieJson, Encoding.UTF8, "application/json");
                    return response;
                    //return Request.CreateResponse(HttpStatusCode.OK, movies);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No movies found");
                }
            }
        }


        [HttpGet]
        public HttpResponseMessage GetAllDramasName()
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var movies = db.Movie.Where(m => m.Type == "Drama").Select(w => new
                {
                    Id = w.Movie_ID,
                    Name = w.Name,
                }).ToList();

                if (movies.Any())
                {
                    string movieJson = JsonConvert.SerializeObject(movies, jsonSettings);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(movieJson, Encoding.UTF8, "application/json");
                    return response;
                    //return Request.CreateResponse(HttpStatusCode.OK, movies);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Drama found");
                }
            }
        }



        [HttpGet]
        public HttpResponseMessage GetAllWriters()
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {

                var writer = db.Writer.ToList<Writer>();
                if (writer != null)
                {


                    return Request.CreateResponse(HttpStatusCode.OK, writer);
                }

                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Writer not found");
                }
            }
        }
        [HttpGet]
        public HttpResponseMessage HistoryAcceptedprojectByEditor(int Editor_ID)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var projects = db.SentProject
                    .Where(s => s.Status == "Accepted" && s.Editor_ID == Editor_ID)
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

        [HttpPost]
        public HttpResponseMessage AcceptSentProject(int sProId)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {

                var existingProject = db.SentProject.FirstOrDefault(s => s.SentProject_ID == sProId);
                if (existingProject == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found");
                }
                // Retrieve actual balance of the writer from the Writer table
                var writerBalance = db.Writer.Where(w => w.Writer_ID == existingProject.Writer_ID)
                                             .Select(w => w.Balance)
                                             .FirstOrDefault();

                existingProject.Status = "Accepted";
                db.SaveChanges();


                var proposal = db.SentProposals
                                  .Where(s => s.SentProposal_ID == existingProject.SentProposal_ID).Select(s => new {
                                      s.Sent_at,
                                      s.Balance,
                                      s.Type
                                  }).FirstOrDefault();

                int? balance = 0;

                if (DateTime.Parse(proposal.Sent_at) <= DateTime.Parse(existingProject.Send_at))
                {
                    balance = proposal.Balance;
                }
                else
                {
                    balance = (proposal.Balance - ((proposal.Balance * 20) / 100));
                }
                balance += writerBalance;
                var writerBalace = db.Writer.Where(w => w.Writer_ID == existingProject.Writer_ID).FirstOrDefault();

                writerBalace.Balance = balance;
                db.SaveChanges();

                if (proposal.Type == "Movie")
                {
                    var clips = db.Clips.Where(c => c.Sent_ID == sProId).ToList();

                    if (clips.Any())
                    {
                        foreach (var clip in clips)
                        {
                            clip.Movie_ID = existingProject.Movie_ID;
                            clip.Sent_ID = null;
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Clips not found");
                    }
                }
                else
                {
                    var clips = db.DramasClips.Where(c => c.Sent_ID == sProId).ToList();

                    if (clips.Any())
                    {
                        foreach (var clip in clips)
                        {
                            clip.Movie_ID = existingProject.Movie_ID;
                            clip.Sent_ID = null;

                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Clips not found");
                    }
                }



                var summary = db.Summary.FirstOrDefault(s => s.Sent_ID == sProId);

                if (summary != null)
                {
                    summary.Movie_ID = existingProject.Movie_ID;
                    summary.Sent_ID = null;
                    db.SaveChanges();

                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Summary not found");
                }

                var setMovie = new GetMovie()
                {
                    GetMovie_ID = GenerateId(),
                    // Clips_ID = clips.Clips_ID,
                    Writer_ID = existingProject.Writer_ID,
                    Movie_ID = existingProject.Movie_ID,
                    Editor_ID = existingProject.Editor_ID,
                    Summary_ID = summary.Summary_ID,

                };
                db.GetMovie.Add(setMovie);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Uploaded");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(ex);
            }
        }


        [HttpPost]

        public HttpResponseMessage SentProposal()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var request = HttpContext.Current.Request;

            int? movieId = Int32.Parse(request["Movie_ID"]);
            int? editorId = Int32.Parse(request["Editor_ID"]);
            int? writerId = Int32.Parse(request["Writer_ID"]);
            string movieName = request["Movie_Name"];
            string[] genreArray = request.Form.GetValues("Genre");
            HashSet<string> uniqueGenres = new HashSet<string>(genreArray);
            string genre = string.Join(",", uniqueGenres);
            int episode = Int32.Parse(request["Episode"]);
            //int Balance = Int32.Parse(request["Balance"]);
            string type = request["Type"];
            string director = request["Director"];
            string dueDate = request["DueDate"];
            DateTime currentDate = DateTime.Now;
            int amount = Int32.Parse(request["Amount"]);

            var imageFile = request.Files["Image"];
            //string coverImage = request["Cover_Image"];

            //return Request.CreateResponse(HttpStatusCode.OK ,"Image :: " +image);
            //var imageBase64 = request["Image"];

            try
            {

                if (movieId != 0)
                {
                    var proposal = new SentProposals()
                    {
                        SentProposal_ID = GenerateId(),
                        Movie_ID = movieId,
                        Editor_ID = editorId,
                        Writer_ID = writerId,
                        Movie_Name = movieName,
                        Image = SaveImageToDisk(imageFile),
                        Cover_Image = request["Cover_Image"],
                        Genre = genre,
                        Type = type,
                        Director = director,
                        DueDate = dueDate,
                        Status = "Sent",
                        Episode = episode,
                        Balance = amount,
                        Sent_at = currentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        Writer_Notification = true


                    };
                    //SaveBase64ImageToDisk(proposal);
                    db.SentProposals.Add(proposal);
                    db.SaveChanges();

                    var response = new
                    {

                        proposal,

                    };
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    int id = GenerateId();

                    var movie = new Movie()
                    {
                        Movie_ID = id,
                        Name = movieName,

                        Category = genre,
                        Type = type,
                        Director = director,
                        anySummaryOrClip = false
                    };


                    //var imageFile = request["Image"];
                    if (imageFile != null)
                    {
                        string imagePath = SaveImageToDisk(imageFile);
                        movie.Image = imagePath;
                    }


                    /*var coverImageFile = request["Cover_Image"];
                    if (coverImageFile != null )
                    {
                        string imagePath = SaveImageToDisk(coverImageFile);
                        movie.CoverImage = imagePath;
                    }*/

                    db.Movie.Add(movie);

                    db.SaveChanges();
                    var proposal = new SentProposals()
                    {
                        SentProposal_ID = GenerateId(),
                        Movie_ID = id,
                        Editor_ID = editorId,
                        Writer_ID = writerId,
                        Movie_Name = movieName,
                        Image = movie.Image,
                        //Cover_Image = movie.CoverImage,
                        Genre = genre,
                        Type = type,
                        Director = director,
                        DueDate = dueDate,
                        Episode = episode,
                        Sent_at = currentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        Balance = amount,
                        Writer_Notification = true,
                        Status = "Sent",
                    };
                    db.SentProposals.Add(proposal);
                    db.SaveChanges();


                    var response = new
                    {
                        movie,
                        proposal
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }






            }
            catch (Exception ex)
            {
                return Request.CreateResponse(ex.Message);
            }

        }

        [HttpGet]
        public HttpResponseMessage ReceiveSentProject(int Editor_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var projects = db.SentProject
                .Where(s => s.Status == "Sent" && s.Editor_ID == Editor_ID)
                .Select(s => new
                {
                    s.SentProject_ID,
                    s.Movie_ID,
                    s.SentProposal_ID,
                    s.Editor_ID,
                    s.Writer_ID,
                    s.Status,

                    s.Send_at,
                    ProposalData = db.SentProposals
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
                        .FirstOrDefault(),
                    Writer_Name = db.Writer.FirstOrDefault(w => w.Writer_ID == s.Writer_ID).UserName,
                })
                .OrderByDescending(s => s.Send_at)
                .ToList();

            var responseContent = new
            {
                Project = projects
            };

            return Request.CreateResponse(HttpStatusCode.OK, responseContent);
        }




        [HttpGet]
        public HttpResponseMessage ViewSentProject(int Movie_ID,int WriterID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            var summary = db.Summary.Where(s => s.Movie_ID == Movie_ID && s.Writer_ID==WriterID).Select(s => new {
                s.Movie_ID,
                s.Summary1,
                s.Writer_ID
            });

            var project = db.SentProject.FirstOrDefault(s => s.Movie_ID == Movie_ID);
            if (project == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "SentProject not found");
            }
            var movieData = db.Movie
                                    .FirstOrDefault(m => m.Movie_ID == project.Movie_ID);
            string mediaType = movieData.Type;

            object clipsData = null;
            if (mediaType == "Movie")
            {
                clipsData = db.Clips
                                .Where(s => s.Movie_ID == Movie_ID && s.Writer_ID==WriterID)
                                .Select(s => new
                                {
                                    s.Clips_ID,
                                    s.Url,
                                    s.End_time,
                                    s.Start_time,
                                    s.Title,
                                    s.isCompoundClip,
                                    s.Description
                                    
                                });
            }
            else
            {
                clipsData = db.DramasClips
                                .Where(s => s.Movie_ID == Movie_ID)
                                .Select(s => new
                                {
                                    s.DramasClip_ID,
                                    s.Url,
                                    s.End_time,
                                    s.Start_time,
                                    s.Title,
                                    s.Episode,
                                    s.isCompoundClip
                                });
            }


            var responseContent = new
            {
                Movie_ID,
                Summary = summary,
                Clips = clipsData
            };


            return Request.CreateResponse(HttpStatusCode.OK, responseContent);
        }






        [HttpGet]
        public HttpResponseMessage FetchSummary(int sentProjectId)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            try
            {
                var project = db.SentProject.FirstOrDefault(s => s.SentProject_ID == sentProjectId);

                if (project == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "SentProject not found");
                }

                var summaryData = db.Summary
                                    .FirstOrDefault(s => s.Sent_ID == project.SentProject_ID);

                var movieData = db.Movie
                                    .FirstOrDefault(m => m.Movie_ID == project.Movie_ID);

                var writerData = db.Writer
                                    .FirstOrDefault(w => w.Writer_ID == project.Writer_ID);

                string mediaType = movieData.Type;

                object clipsData = null;

                if (mediaType == "Movie")
                {
                    clipsData = db.Clips
                                    .Where(c => c.Sent_ID == project.SentProject_ID)
                                    .Select(s => new
                                    {
                                        s.Clips_ID,
                                        s.Url,
                                        s.End_time,
                                        s.Start_time,
                                        s.Title,
                                        s.isCompoundClip
                                    }).OrderBy(s => s.Start_time).ToList();
                }
                else
                {
                    clipsData = db.DramasClips
                                    .Where(c => c.Sent_ID == project.SentProject_ID)
                                    .Select(s => new
                                    {
                                        s.DramasClip_ID,
                                        s.Url,
                                        s.End_time,
                                        s.Start_time,
                                        s.Title,
                                        s.Episode,
                                        s.isCompoundClip
                                    }).OrderBy(s => s.Start_time).ToList();
                }

                var responseData = new
                {
                    SentProject = project,
                    SummaryData = summaryData,
                    MovieData = movieData,
                    WriterData = writerData,
                    ClipsData = clipsData
                };

                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage ShowSentProposals(int editorId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();

                var proposals = db.SentProposals
                                    .Where(s => s.Editor_ID == editorId && s.Status != "Received")
                                    .OrderByDescending(s => s.Sent_at)
                                    .Select(s => new
                                    {
                                        SentProposal_ID = s.SentProposal_ID,
                                        Writer_ID = s.Writer_ID,
                                        Image = s.Image,

                                        WriterName = db.Writer.FirstOrDefault(w => w.Writer_ID == s.Writer_ID).UserName,
                                        Movie_Name = s.Movie_Name,
                                        Director = s.Director,
                                        Type = s.Type,
                                        Genre = s.Genre,
                                        DueDate = s.DueDate,
                                        Status = s.Status
                                    })
                                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, proposals);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }









        [HttpPost]
        public HttpResponseMessage RewriteSentProject(int SentProject_ID, string editorsComment)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var sentProject = db.SentProject.FirstOrDefault(s => s.SentProject_ID == SentProject_ID);
                if (sentProject == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Project not found");
                }


                sentProject.Status = "Rewrite";


                sentProject.EditorComment = editorsComment;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "SentProject status updated to Rewrite");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /* private string SaveImageToDisk(HttpPostedFile imageFile)
         {
             string imagePath = "";
             string fileName = "";
             try
             {
                 if (imageFile != null && imageFile.ContentLength > 0)
                 {
                     fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                     imagePath = Path.Combine("D:\\Project Files\\BlinkMoviesAndDramaCommunity\\images", fileName);
                     imageFile.SaveAs(imagePath);
                 }
                 else
                 {
                     throw new Exception("Image file is null or empty");
                 }
             }
             catch (Exception ex)
             {

                 Console.WriteLine($"Error saving image to disk: {ex.Message}");
                 throw new Exception("Error saving image to disk", ex);
             }

             return fileName;
         }*/
        private string SaveImageToDisk(HttpPostedFile imageFile)
        {
            string imagePath = "";
            string fileName = "";
            try
            {
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    imagePath = Path.Combine("C:\\Users\\home\\Downloads\\BlinkBackend\\BlinkBackend\\Images\\", fileName);
                    imageFile.SaveAs(imagePath);

                }
                else
                {
                    throw new Exception("Image file is null or empty");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error saving image to disk: {ex.Message}");
                throw new Exception("Error saving image to disk", ex);
            }

            return fileName;
        }

        /* private string SaveBase64ImageToDisk(string base64String)
         {
             string base64Data = base64String.Split(',')[1];

             byte[] imageBytes = Convert.FromBase64String(base64Data);

             // Generate a unique filename or use some logic to determine the filename
             string fileName = Guid.NewGuid().ToString() + ".jpg";

             // Specify the path where you want to save the image
             string filePath = Path.Combine("C:\\Users\\home\\Downloads\\BlinkBackend\\BlinkBackend\\Images\\", fileName);

             // Save the image to disk
             File.WriteAllBytes(filePath, imageBytes);

             return fileName;
         }*/

        [HttpDelete]
        public HttpResponseMessage DeleteProposal(int SentProposal_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var proposal = db.SentProposals.Where(p => p.SentProposal_ID == SentProposal_ID).FirstOrDefault();

                if (proposal != null)
                {
                    db.SentProposals.Remove(proposal);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Proposal deleted successfully.");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Proposal not found.");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateEditorNotifications(int editorId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProposals = db.SentProposals.Where(sp => sp.Editor_ID == editorId).ToList();

                if (sentProposals.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProposals found for the specified editor");
                }


                foreach (var sentProposal in sentProposals)
                {
                    sentProposal.Editor_Notification = false;
                }


                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Editor notifications updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetSentProposalsIdsWithEditorNotification(int editorId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProposalIds = db.SentProposals
                                        .Where(sp => sp.Editor_ID == editorId && sp.Editor_Notification == true && sp.Status != "Received")
                                        .Select(sp => new
                                        {
                                            sp.SentProposal_ID,
                                            sp.Status
                                        })
                                        .ToList();

                if (sentProposalIds.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProposals found with Editor_Notification true for the specified editor");
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
        public HttpResponseMessage UpdateAllEditorNotificationsToFalse(int editorId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProposals = db.SentProposals.Where(sp => sp.Editor_ID == editorId).ToList();

                if (sentProposals.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProposals found for the specified editor");
                }


                foreach (var sentProposal in sentProposals)
                {
                    sentProposal.Editor_Notification = false;
                }


                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "All Editor notifications updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetEditorNotificationsSentProject(int editorId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var editorNotifications = db.SentProject
                                            .Where(sp => sp.Editor_Notification == true && sp.Editor_ID == editorId)
                                            .Select(sp => new
                                            {
                                                SentProject_ID = sp.SentProject_ID,
                                                Status = sp.Status
                                            })
                                            .ToList();

                if (editorNotifications.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No editor notifications found for the specified editor");
                }


                int totalCount = editorNotifications.Count;


                var responseData = new
                {
                    EditorNotifications = editorNotifications,
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
        public HttpResponseMessage UpdateAllEditorNotificationstoFalseSentProject(int editorId)
        {
            try
            {
                BlinkMovie2Entities db = new BlinkMovie2Entities();


                var sentProjects = db.SentProject.Where(sp => sp.Editor_ID == editorId).ToList();

                if (sentProjects.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No SentProject records found for the specified editor");
                }


                foreach (var sentProject in sentProjects)
                {
                    sentProject.Editor_Notification = false;
                }


                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "All Editor notifications updated to false for the specified editor");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




        [HttpGet]
        public HttpResponseMessage GetMoviesByEditorId(int editorId, string type)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                try
                {
                    object moviesData;

                    if (type == "Movie")
                    {
                        moviesData = db.GetMovie
                                        .Where(m => m.Editor_ID == editorId)
                                        .Join(db.Movie.Where(movie => movie.Type == "Movie"),
                                                getMovie => getMovie.Movie_ID,
                                                movie => movie.Movie_ID,
                                                (getMovie, movie) => new
                                                {
                                                    Editor_ID = getMovie.Editor_ID,
                                                    MovieID = getMovie.Movie_ID,
                                                    Title = movie.Name,
                                                    Image = movie.Image,
                                                    Type = movie.Type
                                                })
                                        .Distinct()
                                        .ToList();
                    }
                    else
                    {
                        moviesData = db.GetMovie
                                        .Where(m => m.Editor_ID == editorId)
                                        .Join(db.Movie.Where(movie => movie.Type != "Movie"),
                                                getMovie => getMovie.Movie_ID,
                                                movie => movie.Movie_ID,
                                                (getMovie, movie) => new
                                                {
                                                    Editor_ID = getMovie.Editor_ID,
                                                    MovieID = getMovie.Movie_ID,
                                                    Title = movie.Name,
                                                    Image = movie.Image,
                                                    Type = movie.Type
                                                })
                                        .Distinct()
                                        .ToList();
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, moviesData);
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                }
            }
        }


        [HttpGet]
        public HttpResponseMessage GetWritersOfMovies(int editorId, int movieId)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            try
            {

                var writerIds = db.GetMovie
                                    .Where(g => g.Editor_ID == editorId && g.Movie_ID == movieId)
                                    .Select(g => g.Writer_ID)
                                    .ToList();

                if (writerIds.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No writers found for the given Data");
                }


                var writersData = db.Writer
                                    .Where(w => writerIds.Contains(w.Writer_ID))
                                    .Select(w => new
                                    {
                                        w.Writer_ID,
                                        w.UserName,
                                        w.Image
                                    })
                                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, writersData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetAcceptedSummary(int movieId, int writerId)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            try
            {
                var movieData = db.Movie.Where(m => m.Movie_ID == movieId).Select(s => s.Name).FirstOrDefault();

                var summaryData = db.Summary.Where(s => s.Movie_ID == movieId && s.Writer_ID == writerId).Select(s => s.Summary1)
                            .FirstOrDefault(); ;

                if (summaryData == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Summary data not found for the given parameters");
                }



                var responseData = new
                {
                    movieName = movieData,
                    SummaryData = summaryData,

                };

                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetAcceptedSummaryClips(int movieId, int writerId)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            try
            {
                var movieData = db.Movie.Where(m => m.Movie_ID == movieId).Select(s => s.Name).FirstOrDefault();






                var clipsData = db.Clips
                                    .Where(c => c.Movie_ID == movieId && c.Writer_ID == writerId)
                                    .Select(c => new
                                    {
                                        c.Clips_ID,
                                        c.Url,
                                        c.End_time,
                                        c.Start_time,
                                        c.Title,
                                        c.isCompoundClip
                                    })
                                    .OrderBy(c => c.Start_time)
                                    .ToList();
                if (clipsData == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Clips data not found for the given parameters");
                }
                var responseData = new
                {
                    movieName = movieData,
                    ClipsData = clipsData
                };

                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




    }



}
