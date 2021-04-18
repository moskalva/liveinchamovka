module liveinchamovka.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/articles">] Articles
    | [<EndPoint "/article/{id}">] Article of id:int

type ArticleId = int

type ArticleInfo = {
        id : ArticleId
        title: string
        publishDate: DateTime
    }


type Article =
    {
        id : ArticleId
        title: string
        content: string
        publishDate: DateTime
    }

type ContentModel =
| ArticlesModel of ArticleInfo[] option
| ArticleModel of Article option

and Model =
    {
        page: Page
        content: ContentModel
        error: string option
        signedInAs: option<string>
    }

let initModel =
    {
        page = Home
        content = ArticlesModel None
        error = None
        signedInAs = None
    }

/// Remote service definition.
type ArticlesService =
    {
        /// Get the list of all articles in the collection.
        getArticles: unit -> Async<ArticleInfo[]>
        
        /// Get the article content
        getArticle: ArticleId -> Async<Article>

        /// Sign into the application.
        signIn : string * string -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        getUsername : unit -> Async<string>

        /// Sign out from the application.
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/articles"

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | GetArticles 
    | GotArticles of ArticleInfo[]
    | GetArticle of ArticleId
    | GotArticle of Article
    | Error of exn

let update remote message model =
    match message with
    | SetPage page ->
        { model with page = page }, Cmd.none
    | GetArticles -> 
        let cmd = Cmd.OfAsync.either remote.getArticles () GotArticles Error
        {model with content = ArticlesModel None } , cmd
    | GotArticles articles ->
        { model with content = ArticlesModel(Some articles) }, Cmd.none
    | GetArticle id -> 
        let cmd = Cmd.OfAsync.either remote.getArticle id GotArticle Error
        {model with content = ArticleModel None } , cmd
    | GotArticle article ->
        { model with content = ArticleModel(Some article) }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

let articlesView (articles: ArticleInfo[]) (dispatch:Dispatch<Message>)=
    ul [] [
        forEach articles <| fun article -> 
                            li [] [
                                p [] [text article.title]
                                p [] [text (article.publishDate.ToString()) ]
                                button [on.click (fun _ -> dispatch(GetArticle article.id))] [ text "Open"]
                            ]
    ]
    
let articleView (article: Article) (dispatch:Dispatch<Message>)=
    div [] [
        p [] [text article.title]
        p [] [text (article.publishDate.ToString()) ]
        p [] [text article.content]
        button [on.click (fun _ -> dispatch(GetArticles))] [ text "Back"]
    ]

let loadingView = text "Loading..."

let view (model:Model) dispatch =
    match model.content with
    | ArticlesModel articles -> 
        div [] [
            cond articles <| function
                           | None -> loadingView
                           | Some a -> articlesView a dispatch
        ]
    | ArticleModel article -> 
        div [] [
            cond article <| function
                           | None -> loadingView
                           | Some a -> articleView a dispatch
        ]

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let articlesService = this.Remote<ArticlesService>()
        let update = update articlesService
        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetArticles) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
