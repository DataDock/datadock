# DataDock.io

This is the repository for the open-source code base that runs https://datadock.io

## What is DataDock?
DataDock is an online service which creates navigable data portals for individuals and organisations to publish open data free of charge.

## What's a data portal?
A data portal is a place where you make your data available to other people via the web as "open data".

## How does it work?

You access DataDock by visiting the homepage and then logging in using a GitHub account. 

All authentication is done using your GitHub account, as the data you publish will ultimately end up published to GitHub. GitHub is a world leader in providing online storage for developers to work collaboratively on projects. We are leveraging the same tools to allow you to publish data in a similar fashion. You can [read more about GitHub on their website](https://github.com/open-source).

Once you have accessed DataDock using your GitHub account, you can use the Dashboard to upload spreadsheet to your GitHub repositories.

Once you have selected a spreadsheet (the type of spreadsheet file required by DataDock is "CSV"), you are shown a metadata editor where you must select a license. A license is a way of telling people who come across your data what they are allowed to do with it.

The license is the only thing that _you_ must select when publishing data, you can leave everything else as the preselected defaults if you wish. At this point, you can hit "Publish" and DataDock will upload, convert and publish your data on your own data portal.

If you would like to change any more settings you can:
* choose a friendly title (instead of the default filename)
* add a description to give more detail about what the contents of your dataset are, collection methods, areas or time periods covered, etc
* add tags to help the discoverability of the data

There's also a number of other settings to do with data processing and publishing that you can read about in the [documentation](https://github.com/DataDock/datadock/wiki).

### What happens next?

DataDock takes the CSV you have chosen, and the settings that you entered (Title, Description, etc as well as any advanced settings that you chose) and converts all the records in your dataset into RDF, which is the file format used for the "Web of Data".

Once converted into RDF, DataDock is able to create a data portal consisting of:

* a home page listing all the repositories you have used DataDock to publish into
* a repository homepage that lists a table of contents for all the datasets within it
* a dataset landing page showing dataset information, data downloads and links to dataset's resources
* pages for each of the resources in a dataset. If your dataset had 1,000 records then 1,000 pages will be generated so that they can be a part of the **Web of Data** 

You can use the "Explore" buttons on your dashboard to jump over to your newly created data portal.

## What else can I do?

* You can configure your data portal settings to adjust what's produced on your data portal home page, and on the repository home pages. 
* You can set up search buttons that will show along the top of your data portal contents pages - these filter your datasets based on the tags you gave them
* You can adjust your settings as you upload data to make better use of Linked Data practises (by re-using well known property URLs such as on [schema.org](https://schema.org)... but we'll add more documentation / blog posts to help you with that)

## Any hidden features I would be interested in?

This will most likely be of interest to the data publishing nerds... but some hidden features are:

* The metadata file generated (and published as part of the process) uses [CSVW](https://www.w3.org/TR/tabular-data-primer/) which is in turn used to convert CSV to RDF 
* The data portal pages have JSON-LD and RDFA mark-up enhancements to [boost discoverability](https://developers.google.com/search/docs/data-types/dataset)
* All your content resides in your GitHub account, we don't hold your data - we merely process it for you

## Where do I start?

Simply head over to the [DataDock homepage](https://datadock.io) and hit Sign Up. Read the [Getting Started Guide](https://github.com/DataDock/datadock/wiki) for help along the way

