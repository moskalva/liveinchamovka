namespace liveinchamovka.Server

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open liveinchamovka


type ArticlesService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Main.ArticlesService>()

    let articles =
        (let json = Path.Combine(env.ContentRootPath, "data/articles.json") |> File.ReadAllText
        JsonSerializer.Deserialize<Client.Main.Article[]>(json)
        |> ResizeArray).ToArray()
    let articlesInfo = 
        articles |> Array.map(fun article -> {
            Client.Main.ArticleInfo.id = article.id
            Client.Main.ArticleInfo.title = article.title
            Client.Main.ArticleInfo.publishDate = article.publishDate
        }) 
    let articlesById = 
        articles 
        |> Seq.map(fun article-> 
            article.id, article
        ) |> dict

    override this.Handler =
        {
            getArticle = ctx.Authorize <| fun (articleId) -> async {
                return articlesById.[articleId]
            }

            getArticles = ctx.Authorize <| fun () -> async {
                return articlesInfo
            }

            signIn = fun (username, password) -> async {
                if password = "password" then
                    do! ctx.HttpContext.AsyncSignIn(username, TimeSpan.FromDays(365.))
                    return Some username
                else
                    return None
            }

            signOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            getUsername = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }
