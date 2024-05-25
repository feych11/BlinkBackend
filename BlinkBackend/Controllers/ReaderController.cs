using BlinkBackend.Models;
using BlinkBackend.Classes;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using System.Text;

namespace BlinkBackend.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ReaderController : ApiController
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

        public HttpResponseMessage AddReaderFavorites(Favorites f)
        {

            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {


                var favorites = new Favorites()
                {
                    Favorites_ID = GenerateId(),
                    Reader_ID = f.Reader_ID,
                    Writer_ID = f.Writer_ID,
                    Movie_ID = f.Movie_ID,
                };

                db.Favorites.Add(favorites);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Added To Favorites");


            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]

        public HttpResponseMessage AddReaderHistory(History h)
        {

            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {

                var history = new History()
                {
                    History_ID = GenerateId(),
                    Reader_ID = h.Reader_ID,
                    Writer_ID = h.Writer_ID,
                    Movie_ID = h.Movie_ID,
                };

                db.History.Add(history);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Added To History");


            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        public HttpResponseMessage UpdateSubscription(int Reader_ID, string subscription)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);

            if (reader != null)
            {
                reader.Subscription = subscription;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Subscription updated successfully");
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
            }

        }

        [HttpPut]
        public HttpResponseMessage UpdateInterests(int Reader_ID, string newInterests)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);

                if (reader != null)
                {
                    reader.Interest = newInterests;
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Interests updated successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
                }
            }
        }


        [HttpGet]

        public HttpResponseMessage GetAllMovies()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;

            var movies = db.Movie.Where(m => m.Type == "Movie").ToList();
            if (movies != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, movies);
            }

            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }

        }

        [HttpGet]
        public HttpResponseMessage GetAllDramas()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;

            var movies = db.Movie.Where(m => m.Type == "Drama").ToList();
            if (movies != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, movies);
            }

            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }
        }

        [HttpGet]

        public HttpResponseMessage GetSpecificMovie(int Movie_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;

            var movies = db.Movie.FirstOrDefault(m => m.Movie_ID == Movie_ID);
            if (movies != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, movies);
            }

            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }

        }

        [HttpGet]
        public HttpResponseMessage GetMovieDetails(int Movie_ID)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {

                db.Configuration.LazyLoadingEnabled = false;
                var movieIds = db.GetMovie.Where(getMovie => getMovie.Movie_ID == Movie_ID).Select(getMovie => getMovie.Movie_ID);

                var result = db.GetMovie
                    .Where(getMovie => movieIds.Contains(getMovie.Movie_ID))
                    .Join(db.Movie, getMovie => getMovie.Movie_ID, movie => movie.Movie_ID, (getMovie, movie) => new { getMovie, movie })
                    .Join(db.Summary, x => new { x.getMovie.Movie_ID, x.getMovie.Writer_ID }, summary => new { Movie_ID = summary.Movie_ID, Writer_ID = summary.Writer_ID }, (x, summary) => new { x.getMovie, x.movie, summary })
                    .Join(db.Writer, summary => summary.summary.Writer_ID, writer => writer.Writer_ID, (summary, writer) => new { summary.getMovie, summary.movie, summary.summary, writer })
                    .Distinct()
                    .ToList();

                if (result != null)
                {

                    var responseData = result.Select(item => new
                    {
                        Movie = new
                        {
                            item.movie.Movie_ID,
                            item.movie.Name

                        },
                        Writer = new
                        {
                            item.writer.Writer_ID,
                            item.writer.UserName
                        }

                    });
                    return Request.CreateResponse(HttpStatusCode.OK, responseData);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Movie not found");
                }
            }
        }


        [HttpGet]
        public HttpResponseMessage GetSummary(int Writer_ID, int Movie_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            db.Configuration.LazyLoadingEnabled = false;
            var result = db.Summary
                .Where(summary => summary.Writer_ID == Writer_ID && summary.Movie_ID == Movie_ID)
                .Join(db.Writer, summary => summary.Writer_ID, writer => writer.Writer_ID, (summary, writer) => new { summary, writer })
                .Join(db.Movie, x => x.summary.Movie_ID, movie => movie.Movie_ID, (x, movie) => new { x.summary, x.writer, movie })
                .ToList();

            if (result != null && result.Any())
            {
                var responseData = result.Select(item => new
                {
                    Summary = new
                    {
                        item.summary.Summary_ID,
                        item.summary.Summary1
                    },
                    Writer = new
                    {
                        item.writer.Writer_ID,
                        item.writer.UserName
                    },
                    Movie = new
                    {
                        item.movie.Movie_ID,
                    }
                });
                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Movie not found");
            }
        }

        [HttpPut]
        public HttpResponseMessage UpdateReaderPassword(int Reader_ID, string newPassword)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                if (reader == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found");
                }


                reader.Password = newPassword;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Reader password updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateReaderName(int Reader_ID, string newName)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                if (reader == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found");
                }


                reader.UserName = newName;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Reader name updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage SearchMovies(string searchQuery)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {

                var searchResults = db.Movie
                    .Where(m => m.Name.Contains(searchQuery) || m.Category.Contains(searchQuery))
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, searchResults);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetClip(int Writer_ID, int Movie_ID)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();

            db.Configuration.LazyLoadingEnabled = false;
            var result = db.Clips
                .Where(clip => clip.Writer_ID == Writer_ID && clip.Movie_ID == Movie_ID)
                .Join(db.Writer, clip => clip.Writer_ID, writer => writer.Writer_ID, (clip, writer) => new { clip, writer })
                .Join(db.Movie, x => x.clip.Movie_ID, movie => movie.Movie_ID, (x, movie) => new { x.clip, x.writer, movie })
                .ToList();

            if (result != null && result.Any())
            {
                var responseData = result.Select(item => new
                {
                    Clips = new
                    {
                        item.clip.Clips_ID,
                        item.clip.Url,
                        item.clip.Start_time,
                        item.clip.End_time
                    },
                    Writer = new
                    {
                        item.writer.Writer_ID,
                    },
                    Movie = new
                    {
                        item.movie.Movie_ID,
                    },
                });
                return Request.CreateResponse(HttpStatusCode.OK, responseData);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Clip not found");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetFavoriteDetails(int Reader_ID)
        {
            try
            {
                using (var db = new BlinkMovie2Entities())
                {
                    var result = db.Favorites
                        .Where(fav => fav.Reader_ID == Reader_ID)
                        .Join(db.Writer, fav => fav.Writer_ID, writer => writer.Writer_ID, (fav, writer) => new { fav, writer })
                        .Join(db.Movie, x => x.fav.Movie_ID, movie => movie.Movie_ID, (x, movie) => new { x.fav, x.writer, movie })
                        .ToList();

                    var details = result.Select(x => new
                    {
                        ReaderId = x.fav.Reader_ID,
                        WriterId = x.fav.Writer_ID,
                        MovieId = x.fav.Movie_ID,
                        WriterName = x.writer.UserName,
                        MovieTitle = x.movie.Name,
                        Director = x.movie.Director,
                        MovieRating = x.movie.Rating,
                        WriterRating = x.writer.Rating

                    }).ToList<object>();

                    return Request.CreateResponse(HttpStatusCode.OK, details);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetHistoryDetails(int Reader_ID)
        {
            try
            {
                using (var db = new BlinkMovie2Entities())
                {
                    var result = db.History
                        .Where(history => history.Reader_ID == Reader_ID)
                        .Join(db.Writer, history => history.Writer_ID, writer => writer.Writer_ID, (history, writer) => new { history, writer })
                        .Join(db.Movie, x => x.history.Movie_ID, movie => movie.Movie_ID, (x, movie) => new { x.history, x.writer, movie })
                        .ToList();

                    var details = result.Select(x => new
                    {
                        ReaderId = x.history.Reader_ID,
                        WriterId = x.history.Writer_ID,
                        MovieId = x.history.Movie_ID,
                        WriterName = x.writer.UserName,
                        MovieTitle = x.movie.Name,
                        Director = x.movie.Director,
                        MovieRating = x.movie.Rating,
                        WriterRating = x.writer.Rating
                    }).ToList<object>();

                    return Request.CreateResponse(HttpStatusCode.OK, details);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]

        public HttpResponseMessage GetTopRatedMovies()
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            db.Configuration.LazyLoadingEnabled = false;

            var movies = db.Movie.Where(m => m.Rating > 4.0).ToList<Movie>();
            if (movies != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, movies);
            }

            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Data not found");
            }

        }

        [HttpGet]
        public HttpResponseMessage GetMoviesByInterests(int Reader_ID)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                db.Configuration.LazyLoadingEnabled = false;


                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);

                if (reader == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found");
                }


                string[] readerInterests = reader.Interest.Split(',');


                var movies = db.Movie
                    .Where(m => m.Category != null)
                    .ToList()
                    .Where(m => m.Category.Split(',').Any(g => readerInterests.Contains(g)))
                    .ToList();

                if (movies.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, movies);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No movies found based on reader's interests");
                }
            }
        }


        [HttpPost]
        public HttpResponseMessage RateWriter(int Reader_ID, int Writer_ID, float writerRate)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                db.Configuration.LazyLoadingEnabled = false;


                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                if (reader == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found");
                }


                var writer = db.Writer.FirstOrDefault(w => w.Writer_ID == Writer_ID);
                if (writer == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Writer not found");
                }

                var existingWriterRating = db.ReaderRate.FirstOrDefault(r => r.Reader_ID == Reader_ID && r.Writer_ID == Writer_ID);

                if (existingWriterRating != null)
                {

                    writer.Rating = ((writer.Rating * writer.Rating) - existingWriterRating.Writer_Rating) / 5;
                    writer.Rating -= 1;


                    db.ReaderRate.Remove(existingWriterRating);
                }

                var writerRateEntity = new ReaderRate
                {
                    ReaderRate_ID = GenerateId(),
                    Reader_ID = Reader_ID,
                    Writer_ID = Writer_ID,
                    Writer_Rating = writerRate
                };

                db.ReaderRate.Add(writerRateEntity);
                db.SaveChanges();

                writer.Rating = ((writer.Rating * writer.Rating) + writerRate) / 5;
                writer.Rating += 1;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Writer rating successfully submitted");
            }
        }


        [HttpPost]
        public HttpResponseMessage RateMovie(int Reader_ID, int Movie_ID, float movieRate)
        {
            BlinkMovie2Entities db = new BlinkMovie2Entities();
            try
            {
                var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                if (reader == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Reader Not Found");
                }

                var movie = db.Movie.FirstOrDefault(m => m.Movie_ID == Movie_ID);
                if (movie == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Movie Not Found");
                }

                var existingReaderRating = db.ReaderRate.FirstOrDefault(r => r.Reader_ID == Reader_ID && r.Movie_ID == Movie_ID);

                if (existingReaderRating != null)
                {
                    movie.Rating = (movie.Rating * movie.TotalNoOfRatings - existingReaderRating.Movie_Rating) / (movie.TotalNoOfRatings - 1);
                    movie.TotalNoOfRatings -= 1;
                    db.ReaderRate.Remove(existingReaderRating);
                }

                var newReaderRating = new ReaderRate
                {
                    ReaderRate_ID = GenerateId(),
                    Reader_ID = reader.Reader_ID,
                    Movie_ID = movie.Movie_ID,
                    Movie_Rating = movieRate
                };
                db.ReaderRate.Add(newReaderRating);

                var rating = movie.Rating = (movie.Rating * movie.TotalNoOfRatings + movieRate) / (movie.TotalNoOfRatings + 1);
                movie.TotalNoOfRatings += 1;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Movie Rating Updated");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage IssueFreeMovie(int readerId)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                try
                {
                    var lastIssuedDate = db.FreeMovie
                                        .Where(fm => fm.Reader_ID == readerId)
                                        .OrderByDescending(fm => fm.issueDate)
                                        .Select(fm => fm.issueDate)
                                        .FirstOrDefault();

                    string todayDateString = DateTime.Today.ToString("yyyy-MM-dd");
                    bool hasDayPassed = (lastIssuedDate != null && DateTime.Parse(lastIssuedDate) < DateTime.Today);

                    var readerIssuedFreeMovie = db.FreeMovie.Where(fm => fm.Reader_ID == readerId).ToList();

                    if (hasDayPassed || lastIssuedDate == null)
                    {
                    again:
                        var randomSummary = db.Summary
                                              .OrderBy(r => Guid.NewGuid())
                                              .FirstOrDefault();

                        if (lastIssuedDate == null)
                        {
                            randomSummary = db.Summary
                                              .OrderBy(r => Guid.NewGuid())
                                              .FirstOrDefault();

                            if (randomSummary == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, "No available summaries to issue.");
                            }
                        }

                        // Check if the status of the movie is "Accepted"
                        var projectStatus = db.SentProject
                                              .Where(sp => sp.Movie_ID == randomSummary.Movie_ID)
                                              .Select(sp => sp.Status)
                                              .FirstOrDefault();

                        if (projectStatus != "Accepted")
                        {
                            goto again; // Find another random summary if the status is not "Accepted"
                        }

                        if (randomSummary.Movie_ID == null)
                        {
                            goto again;
                        }

                        if (readerIssuedFreeMovie.Count == db.Summary.Count())
                        {
                            var recordsToDelete = db.FreeMovie.Where(fm => fm.Reader_ID == readerId);

                            db.FreeMovie.RemoveRange(recordsToDelete);
                            db.SaveChanges();
                        }

                        foreach (var issuedMovie in readerIssuedFreeMovie)
                        {
                            if (issuedMovie.Movie_ID == randomSummary.Movie_ID && issuedMovie.Writer_ID == randomSummary.Writer_ID)
                            {
                                goto again;
                            }
                        }

                        var movie = db.Movie.Where(m => m.Movie_ID == randomSummary.Movie_ID).Select(s => new
                        {
                            s.Movie_ID,
                            s.Name,
                            s.Image,
                            s.CoverImage,
                            s.Type
                        }).FirstOrDefault();

                        var writer = db.Writer.Where(w => w.Writer_ID == randomSummary.Writer_ID).Select(s => new
                        {
                            s.Writer_ID,
                            s.UserName,
                        }).FirstOrDefault();

                        FreeMovie newFreeMovie = new FreeMovie
                        {
                            FreeMovie_ID = GenerateId(),
                            Movie_ID = randomSummary.Movie_ID,
                            Writer_ID = randomSummary.Writer_ID,
                            issueDate = todayDateString,
                            Reader_ID = readerId,
                            Episode = randomSummary.Episode,
                        };

                        db.FreeMovie.Add(newFreeMovie);
                        db.SaveChanges();

                        var result = new
                        {
                            Movie = movie,
                            Writer = writer,
                            Episode = randomSummary.Episode,
                            IssuedMovie = newFreeMovie
                        };

                        string resultJson = JsonConvert.SerializeObject(result, jsonSettings);

                        HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                        response.Content = new StringContent(resultJson, Encoding.UTF8, "application/json");

                        return response;
                    }
                    else
                    {
                        var lastIssuedMovie = db.FreeMovie
                                                .Where(fm => fm.Reader_ID == readerId && fm.issueDate == lastIssuedDate)
                                                .FirstOrDefault();

                        var movie = db.Movie.Where(m => m.Movie_ID == lastIssuedMovie.Movie_ID).Select(s => new
                        {
                            s.Movie_ID,
                            s.Name,
                            s.Image,
                            s.CoverImage,
                            s.Type
                        }).FirstOrDefault();

                        var writer = db.Writer.Where(w => w.Writer_ID == lastIssuedMovie.Writer_ID).Select(s => new
                        {
                            s.Writer_ID,
                            s.UserName,
                        }).FirstOrDefault();

                        var result = new
                        {
                            issueDate = lastIssuedDate,
                            Movie = movie,
                            Writer = writer,
                            Episode = lastIssuedMovie.Episode,
                        };

                        string resultJson = JsonConvert.SerializeObject(result, jsonSettings);

                        HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                        response.Content = new StringContent(resultJson, Encoding.UTF8, "application/json");

                        return response;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while processing the request.");
                }
            }
        }





        [HttpGet]
        public HttpResponseMessage IssuePaidMovie()
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                var projects = db.SentProject
                    .Where(s => s.Status == "Accepted" )
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
                            WriterData=db.Writer.Where(sp=>sp.Writer_ID==s.Writer_ID).Select(sp=>new
                            {
                                sp.UserName,
                            }).FirstOrDefault(),

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
        public HttpResponseMessage RechargeBalance(int Reader_ID, int newBalance)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                try
                {
                    var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                    if (reader == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Reader not found");
                    }

                    reader.Subscription = "Paid";
                    reader.Balance = (reader.Balance ?? 0) + newBalance;
                    db.SaveChanges();

                    // Start a background task to reset the balance and subscription after 30 days
                    Task.Run(async () => await ResetBalanceAndSubscription(reader.Reader_ID));

                    return Request.CreateResponse(HttpStatusCode.OK, "Balance recharged successfully");
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                }
            }
        }

        private async Task ResetBalanceAndSubscription(int Reader_ID)
        {
            using (BlinkMovie2Entities db = new BlinkMovie2Entities())
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                try
                {
                    var reader = db.Reader.FirstOrDefault(r => r.Reader_ID == Reader_ID);
                    if (reader != null)
                    {
                        reader.Balance = 0;
                        reader.Subscription = "Free";
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                }
            }
        }





    }
}


//Free Movie

//in Last
//Balance
//GetNotified

