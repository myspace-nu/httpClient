# httpClient

A generic http client class

## Installation

Add a reference to the class .dll in your project. For convenience, also add the namespace.

	using mySpace.httpClient;

## Examples

### GET request

	httpRequest myhttp = new httpRequest();
	myhttp.URI = "http://httpbin.org/get";
	httpResponse resp = myhttp.get();
	Console.Write(resp.content);

### POST request

	httpRequest myhttp = new httpRequest();
	myhttp.URI = "http://httpbin.org/post";
	myhttp.formFields.Add("Form field 1", "Some content");
	myhttp.formFields.Add("Form field 2", "Some more content");
	httpResponse resp = myhttp.post();
	Console.Write(resp.content);

### POST request - Sending a file using multipart/form-data

	httpRequest myhttp = new httpRequest();
	myhttp.contentType = "multipart/form-data";
	httpFile htfile = new httpFile("myFile", @"C:\myFile.txt");
	myhttp.uploadFiles.Add(htfile);
	myhttp.URI = "http://httpbin.org/post";
	httpResponse resp = myhttp.post();
	Console.Write(resp.content);

### Basic access authentication

	httpRequest myhttp = new httpRequest();
	myhttp.username = "user";
	myhttp.password = "passwd";
	myhttp.URI = "http://httpbin.org/basic-auth/user/passwd";
	httpResponse resp = myhttp.get();
	Console.Write(resp.content);
	
### Using cookies

	httpRequest myhttp = new httpRequest();
	myhttp.URI = "http://httpbin.org/cookies";
	myhttp.cookies.Add(new httpCookie("My cookie", "has a value"));
	httpResponse resp = myhttp.get();
	Console.Write(resp.content);

### Posting JSON to a REST API

	var root = new
	{
		title = "foo",
		body = "bar",
		userId = 1
	};
	httpRequest myhttp = new httpRequest();
	myhttp.URI = "https://jsonplaceholder.typicode.com/posts";
	myhttp.headers.Add("Accept", "application/json");
	myhttp.contentType = "application/json; charset=UTF-8";
	myhttp.content = JsonConvert.SerializeObject(root);
	httpResponse resp = myhttp.post();
	Console.Write(resp.content);