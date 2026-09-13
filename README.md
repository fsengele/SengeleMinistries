# Sengele Ministries

Sengele Ministries is an ASP.NET Core MVC web application.

The purpose of this project is to create a modern website for Sengele Ministries.

The website allows visitors to learn about the ministry, watch messages, see events, send prayer requests, create a member account, and shop for ministry resources.

The website also has an Admin area to manage information.

---

## Project Features

### Home Page

The Home page introduces Sengele Ministries.

It includes:

- Ministry information
- Hope and prayer sections
- Latest message
- Events
- Prayer request link
- Links to other pages

---

## About Page

The About page explains:

- The story of Sengele Ministries
- The mission of the ministry
- The vision of the ministry
- What we believe

---

## Messages

Visitors can see ministry messages and teachings.

They can use the Messages page to find Christian messages and resources.

---

## Events

The Events page shows ministry events.

Visitors can see information about upcoming ministry activities and meetings.

---

## Prayer Requests

Visitors can send a prayer request.

The prayer request form allows a person to enter information and send a message to the ministry.

Prayer requests can be saved in the database.

---

## Contact

Visitors can contact Sengele Ministries using the Contact page.

Contact messages can be saved in the database.

---

## Member Registration

A visitor can create a member account.

The registration form includes information such as:

- First Name
- Last Name
- Email
- Password
- Confirm Password

Member information is stored in the database.

---

## Member Login

Registered members can log in to their account.

The application uses authentication to identify the user.

---

## Admin Area

The website has a protected Admin area.

Only an Admin user can access Admin features.

The Admin can manage website information.

---

## Product Management

The Admin can manage products.

The Admin can:

- Add a product
- View products
- Edit a product
- Delete a product
- Set a product price
- Add a product image
- Select a category
- Manage stock quantity
- Publish or unpublish a product

Product information is stored in SQL Server.

---

## Shop

Visitors and members can use the Shop.

The Shop includes different categories such as:

- Books and Bibles
- Teachings and Resources
- Event Collections
- Apparel

A customer can open a product to see more information.

---

## Shopping Cart

Customers can add products to the shopping cart.

The cart allows the customer to:

- Add a product
- View products in the cart
- Change quantity
- Remove a product
- Clear the cart
- See the subtotal and total
- Continue shopping

The shopping cart uses Session to keep the products while the customer is using the website.

---

## Checkout

The project is being prepared for checkout and online payment.

The shopping process is:

Shop → Product Details → Add to Cart → Cart → Checkout → Payment

---

## English and French

The website supports two languages:

- English
- French

The user can change the language from the navigation menu.

The application uses ASP.NET Core Localization.

The selected language is saved using a cookie.

---

## Database

This project uses:

- SQL Server LocalDB
- Entity Framework Core
- Code First Migrations

The database stores information such as:

- Members
- Products
- Contact Messages
- Prayer Requests
- Volunteer Applications

More tables can be added as the project grows.

---

## Technologies Used

This project uses:

- C#
- ASP.NET Core MVC
- .NET
- Razor Views
- HTML
- CSS
- JavaScript
- Entity Framework Core
- SQL Server
- Bootstrap
- Git
- GitHub
- Visual Studio

---

## Project Structure

The project follows the MVC pattern.

### Models

Models represent the application data.

Examples:

- Member
- Product
- CartItem
- ContactMessage
- PrayerRequest
- Event
- Message

### Views

Views display information to the user.

They use Razor, HTML, and CSS.

### Controllers

Controllers receive requests and connect the Models and Views.

Examples:

- HomeController
- MemberController
- ShopController
- CartController
- EventsController
- PrayerController
- ContactController

---

## Security

The project uses authentication and authorization.

Some pages are protected.

For example, Product Management is for Admin users.

Customers can see products in the Shop without getting access to Admin Product Management.

---

## Responsive Design

The website is designed to work on different screen sizes.

It can be used on:

- Desktop computers
- Laptops
- Tablets
- Mobile phones

---

## Future Features

Some features can be added in the future:

- Complete Checkout
- Online Payment
- Order History
- Email Notifications
- More Admin tools
- Mobile Application with .NET MAUI

---

## Project Goal

The goal of this project is to build a professional website for Sengele Ministries and practice real web development.

This project helps me practice:

- MVC
- C#
- Databases
- CRUD operations
- Authentication
- Authorization
- Shopping Cart
- Localization
- Git and GitHub
- Web Design

---

## Developer

**Francis Sengele**

Computer Programming / Web Development Student

---

## Sengele Ministries

**Hope. Prayer. The Word.**
