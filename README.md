# Restaurant Management System

## 📌 Project Overview
The **Restaurant Management System** is a web-based application built using **ASP.NET Core** that helps restaurant owners manage reservations, orders, staff, and customer feedback efficiently. The system provides an intuitive user interface for both customers and administrators to interact seamlessly.

---

## 🎯 Features
- **User Authentication:** Customers and staff can register and log in.
- **Reservation Management:** Customers can book tables online.
- **Menu Management:** Admins can add, update, and remove menu items.
- **Order Processing:** Customers can place orders, and staff can manage them.
- **Staff Management:** Admins can add and manage restaurant staff.
- **Feedback System:** Customers can leave feedback on their dining experience.
- **Dashboard for Admins:** A dedicated dashboard to monitor restaurant operations.
- **Payment Processing:** Secure online payment integration.

---

## 🛠️ Technologies Used
- **Backend:** ASP.NET Core 8.0, Entity Framework Core
- **Frontend:** Razor Pages, Bootstrap, jQuery
- **Database:** Microsoft SQL Server
- **Authentication:** ASP.NET Identity
- **Other:** AJAX, JavaScript, CSS

---

## 🚀 Installation & Setup
### Prerequisites
Ensure you have the following installed:
- **.NET 8.0 SDK** ([Download Here](https://dotnet.microsoft.com/en-us/download))
- **SQL Server & SQL Server Management Studio (SSMS)**
- **Visual Studio 2022** (or any .NET-supported IDE)

### Steps to Run the Project
1. **Clone the Repository:**
   ```sh
   git clone https://github.com/your-repo/restaurant-system.git
   cd restaurant-system
   ```
2. **Setup the Database:**
   - Update `appsettings.json` with your SQL Server connection string.
   - Run the following command to apply migrations:
     ```sh
     dotnet ef database update
     ```
3. **Run the Application:**
   ```sh
   dotnet run
   ```
4. Open `https://localhost:5001` in your browser.

---

## 🎮 Usage
### Admin Panel
- Login as an admin to manage menu, orders, reservations, and staff.
- Accessible at `/Admin/AdminDashboard`

### Customer
- Register/Login to book tables, place orders, and give feedback.
- Accessible at `/Home/Index`

---

## ⚙️ Database Migrations
If you need to make changes to the database schema, follow these steps:
1. Modify the `ApplicationDbContext` or any model.
2. Run:
   ```sh
   dotnet ef migrations add MigrationName
   dotnet ef database update
   ```

---

## 🤝 Contribution Guidelines
1. Fork the repository.
2. Create a new branch: `git checkout -b feature-branch`
3. Make your changes and commit them.
4. Push to the branch: `git push origin feature-branch`
5. Open a Pull Request.

---

## 📜 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 📞 Contact
For any inquiries or issues, feel free to reach out via email or open an issue in the repository.

Happy Coding! 🚀

