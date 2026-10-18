ระบบจัดเก็บและจัดการเอกสารภายในองค์กรอย่างปลอดภัย พัฒนาขึ้นเพื่อช่วยให้ผู้ใช้งานจัดเก็บ ค้นหา ดาวน์โหลด และจัดการเอกสารได้จากศูนย์กลาง พร้อมควบคุมสิทธิ์การเข้าถึงตามบทบาทและแผนก รวมถึงบันทึกประวัติการใช้งานระบบ

> โปรเจกต์เพื่อการศึกษาและสาธิตการพัฒนาเว็บแอปพลิเคชันด้วย ASP.NET, C# และ SQL Server

## Features

- **Authentication** — เข้าสู่ระบบและออกจากระบบด้วยบัญชีผู้ใช้
- **Role-based access control** — แยกสิทธิ์ผู้ดูแลระบบ ฝ่ายขาย และจัดซื้อ
- **Document management** — อัปโหลด ค้นหา ดูรายการ และดาวน์โหลดเอกสาร
- **Department-based access** — จำกัดการเข้าถึงเอกสารตามแผนกและสิทธิ์ที่กำหนด
- **User management** — จัดการบัญชีผู้ใช้สำหรับผู้ดูแลระบบ
- **Audit logs** — บันทึกกิจกรรมสำคัญ เช่น การเข้าสู่ระบบ อัปโหลด และดาวน์โหลด
- **Dashboard** — แสดงภาพรวมเอกสาร ผู้ใช้งาน และกิจกรรมล่าสุด

> รายการฟังก์ชันและสิทธิ์การเข้าถึงอาจแตกต่างกันตามส่วนที่พัฒนาและตั้งค่าไว้ในเวอร์ชันปัจจุบัน

## Technology Stack

- **Frontend:** HTML, CSS, JavaScript
- **Backend:** C#, ASP.NET Generic Handlers (`.ashx`)
- **Framework:** .NET Framework 4.7.2
- **Database:** Microsoft SQL Server / SQL Server Express
- **Development tools:** Visual Studio, SQL Server Management Studio (SSMS)

## Project Structure

```text
KA_DMS/
├── Backend/
│   ├── Db.cs
│   ├── JsonHelper.cs
│   ├── PasswordHelper.cs
│   └── AuthHelper.cs
├── App_Data/
│   └── Uploads/              # ไฟล์เอกสารที่อัปโหลด (ไม่ควร commit)
├── LoginHandler.ashx
├── LogoutHandler.ashx
├── MeHandler.ashx
├── DocumentsListHandler.ashx
├── UploadHandler.ashx
├── DownloadHandler.ashx
├── UsersHandler.ashx
├── DepartmentsHandler.ashx
├── AuditLogHandler.ashx
├── DashboardHandler.ashx
├── login.html
├── dashboard.html
├── documents.html
├── upload.html
├── users.html
├── audit-logs.html
├── style.css
├── app.js
├── Web.config
└── README.md
```

โครงสร้างข้างต้นเป็นตัวอย่างของไฟล์หลัก อาจมีไฟล์อื่นเพิ่มเติมตามเวอร์ชันของโปรเจกต์

## Requirements

ก่อนเริ่มใช้งาน ควรติดตั้ง:

1. **Visual Studio** ที่รองรับการพัฒนา ASP.NET บน .NET Framework
2. **.NET Framework 4.7.2 Developer Pack / Targeting Pack**
3. **SQL Server Express** หรือ SQL Server instance ที่ใช้งานได้
4. **SQL Server Management Studio (SSMS)** สำหรับจัดการฐานข้อมูล (แนะนำ)
5. เว็บเบราว์เซอร์ เช่น Google Chrome หรือ Microsoft Edge

## Setup and Run

### 1. Clone the repository

```bash
git clone <YOUR_GITHUB_REPOSITORY_URL>
```

หรือดาวน์โหลดโปรเจกต์เป็น ZIP จาก GitHub แล้วแตกไฟล์

### 2. Open the project in Visual Studio

- เปิด Visual Studio
- เลือก **File > Open > Web Site** หรือเปิดโปรเจกต์ตามรูปแบบที่จัดเก็บไว้ใน repository
- เลือกโฟลเดอร์โปรเจกต์ `KA_DMS`
- ตรวจสอบว่าเครื่องมี .NET Framework 4.7.2 Targeting Pack

> หาก repository ไม่มีไฟล์ Solution (`.sln`) ให้เปิดเป็น Web Site หรือสร้าง Solution/Project ให้ตรงกับรูปแบบโปรเจกต์ก่อนใช้งาน

### 3. Create the database

1. เปิด SQL Server Management Studio (SSMS)
2. เชื่อมต่อ SQL Server instance ของเครื่อง
3. สร้างฐานข้อมูลชื่อ `KAScienceDMS` หรือใช้ชื่อที่กำหนดใน connection string
4. รัน SQL script สำหรับสร้างตารางและข้อกำหนดของฐานข้อมูล

**หมายเหตุ:** ต้องเพิ่มไฟล์ SQL สำหรับสร้าง schema เช่น `Database/schema.sql` ลงใน repository หากต้องการให้ผู้ใช้คนอื่นตั้งค่าฐานข้อมูลได้สะดวก อย่าอัปโหลดฐานข้อมูลจริงที่มีข้อมูลส่วนตัวหรือข้อมูลที่ไม่ควรเปิดเผย

### 4. Configure the connection string

ตั้งค่า connection string ใน `Web.config` ให้ตรงกับ SQL Server instance ของเครื่องผู้ใช้ ตัวอย่างสำหรับ SQL Server Express ที่ใช้ Windows Authentication:

```xml
<connectionStrings>
  <add name="KAScienceDMS"
       connectionString="Data Source=YOUR_SERVER_NAME\SQLEXPRESS;Initial Catalog=KAScienceDMS;Integrated Security=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

เปลี่ยน `YOUR_SERVER_NAME\SQLEXPRESS` ให้ตรงกับชื่อ SQL Server ของคุณ และตรวจสอบว่าบัญชี Windows ที่ใช้รันแอปมีสิทธิ์เข้าถึงฐานข้อมูล

**อย่าใส่รหัสผ่านจริง, API keys หรือข้อมูลลับลงใน repository สาธารณะ**

### 5. Run the application

- ตรวจสอบว่า `Web.config` ใช้ .NET Framework 4.7.2
- ตรวจสอบ connection string และฐานข้อมูล
- เลือกหน้า `login.html` หรือหน้าเริ่มต้นที่โปรเจกต์ตั้งค่าไว้
- กด Run ใน Visual Studio และทดสอบการเข้าสู่ระบบ

หากเกิดข้อผิดพลาด ให้ตรวจสอบ Output/Error List ของ Visual Studio, การเชื่อมต่อ SQL Server และชื่อ handler ที่หน้าเว็บเรียกใช้

## Demo Data

ในเครื่องพัฒนาสามารถสร้างข้อมูลทดสอบสำหรับตรวจสอบการทำงานของระบบได้

- ข้อมูล Demo ที่มีอยู่ในฐานข้อมูลของเครื่องหนึ่ง **จะไม่ติดไปกับ GitHub โดยอัตโนมัติ**
- ไฟล์เอกสารที่อยู่ใน `App_Data/Uploads/` จะไม่ถูกอัปโหลด หากโฟลเดอร์นี้อยู่ใน `.gitignore`
- หากต้องการให้ผู้อื่นทดลองระบบได้เหมือนกัน ควรจัดเตรียม SQL script สำหรับข้อมูลทดสอบและวิธีสร้างไฟล์ตัวอย่างที่ปลอดภัย
- อย่าเผยแพร่ endpoint สำหรับ seed ข้อมูลที่เปิดให้เรียกใช้งานได้บนเว็บจริง

### Demo account (ถ้าเปิดใช้ชุดข้อมูล Demo)

| Role | Username | Password |
|---|---|---|
| Sales | `demo_sales` | `Demo@12345` |
| Purchasing | `demo_purchasing` | `Demo@12345` |

บัญชีข้างต้นเป็นบัญชีทดสอบที่สร้างโดยชุด Demo ในเครื่องพัฒนาเท่านั้น อาจไม่มีอยู่ในฐานข้อมูลของผู้ที่ clone โปรเจกต์ใหม่ และควรเปลี่ยนหรือลบก่อนใช้งานจริง ห้ามใช้รหัสผ่านนี้กับบัญชีจริง

## Security Notes

- รหัสผ่านควรถูกเก็บในรูปแบบ hash ไม่ใช่ plain text
- ตรวจสอบสิทธิ์ฝั่ง server ทุกครั้งก่อนอ่าน ดาวน์โหลด อัปโหลด หรือลบเอกสาร
- จำกัดชนิดและขนาดไฟล์ที่อนุญาตให้อัปโหลด
- อย่าเปิดเผยไฟล์เอกสารส่วนตัว, ข้อมูลผู้ใช้, connection string ที่มีข้อมูลลับ หรือไฟล์ฐานข้อมูลจริง
- บันทึก Audit Log สำหรับกิจกรรมสำคัญและหลีกเลี่ยงการบันทึกข้อมูลลับลง log
- ก่อนนำไปใช้งานจริง ควรตรวจสอบเรื่อง HTTPS, session security, input validation, error handling และการตั้งค่าสิทธิ์ของฐานข้อมูลเพิ่มเติม

## Current Limitations

- การตั้งค่าฐานข้อมูลต้องทำบนเครื่องผู้ใช้งาน
- ข้อมูลและไฟล์ Demo ในเครื่องผู้พัฒนาไม่ได้ถูกซิงก์ไปยังเครื่องอื่นโดยอัตโนมัติ
- โปรเจกต์นี้จัดทำขึ้นเพื่อการศึกษาและสาธิต ยังไม่ควรนำไปใช้กับเอกสารสำคัญในระบบ Production โดยไม่มีการตรวจสอบความปลอดภัยเพิ่มเติม

## Future Improvements

- เพิ่ม SQL schema และ seed scripts สำหรับติดตั้งระบบใหม่ได้ง่ายขึ้น
- เพิ่มการแบ่งหน้าและตัวกรองเอกสาร
- เพิ่มการทดสอบสิทธิ์การเข้าถึงสำหรับแต่ละ Role และ Department
- เพิ่มการแจ้งเตือนและรายงานกิจกรรม
- เพิ่มการตรวจจับกิจกรรมผิดปกติจาก Audit Logs

## Author

**KA DMS — Secure Document Management System**

Developed as a student project.

---

หากนำ repository นี้ไปพัฒนาต่อ โปรดตรวจสอบการตั้งค่าฐานข้อมูลและสิทธิ์การเข้าถึงก่อนใช้งานทุกครั้ง
