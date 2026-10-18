
async function checkLogin() {
    const protectedPages = [
        "dashboard.html",
        "documents.html",
        "upload.html",
        "users.html",
        "audit-logs.html"
    ];

    const currentPage =
        window.location.pathname.split("/").pop().toLowerCase();

    if (!protectedPages.includes(currentPage)) {
        return;
    }

    try {
        const response = await fetch("Backend/MeHandler.ashx", {
            credentials: "same-origin",
            cache: "no-store"
        });

        if (response.status === 401) {
            window.location.replace("login.html");
            return;
        }

        const result = await response.json();

        if (!response.ok || !result.success) {
            window.location.replace("login.html");
            return;
        }

        // ใส่ชื่อผู้ใช้ในส่วนที่มี element นี้

        const name = result.user.fullName || result.user.username || "ผู้ใช้";
        const role = result.user.role || "ผู้ใช้งาน";

        // รองรับทั้ง ID เดิมและ ID ของ Dashboard
        ["currentUserName", "dashboardUserName"].forEach(function (id) {
            const element = document.getElementById(id);
            if (element) element.textContent = name;
        });

        ["currentUserRole", "dashboardUserRole"].forEach(function (id) {
            const element = document.getElementById(id);
            if (element) element.textContent = role;
        });

        const avatar = document.getElementById("dashboardAvatar");
        if (avatar) {
            avatar.textContent = Array.from(name.trim()).slice(0, 2).join("");
        }


    } catch (error) {
        // หากติดต่อ Handler ไม่ได้ ไม่ควรถือว่าผู้ใช้ผ่านการตรวจสอบ
        window.location.replace("login.html");
    }
}

document.addEventListener("DOMContentLoaded", function () {
    checkLogin();
});



async function loadAuditLogs() {
    const tbody = document.getElementById("auditLogsTableBody");

    if (!tbody) return;

    try {
        const response = await fetch("Backend/AuditLogHandler.ashx", {
            credentials: "same-origin"
        });

        if (response.status === 401) {
            window.location.href = "login.html";
            return;
        }

        if (response.status === 403) {
            tbody.innerHTML =
                '<tr><td colspan="6">เฉพาะ Admin เท่านั้นที่ดูประวัติได้</td></tr>';
            return;
        }

        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error("โหลดประวัติไม่สำเร็จ");
        }

        tbody.replaceChildren();

        if (result.data.length === 0) {
            tbody.innerHTML =
                '<tr><td colspan="6">ยังไม่มีประวัติการใช้งาน</td></tr>';
            return;
        }

        result.data.forEach(function (log) {
            const tr = document.createElement("tr");

            const values = [
                log.createdAt,
                log.username,
                log.action,
                log.documentName || log.details || "-",
                log.ipAddress || "-",
                log.result
            ];

            values.forEach(function (value) {
                const td = document.createElement("td");
                td.textContent = value || "-";
                tr.appendChild(td);
            });

            tbody.appendChild(tr);
        });
    } catch (error) {
        tbody.innerHTML =
            '<tr><td colspan="6">ไม่สามารถโหลดประวัติการใช้งานได้</td></tr>';
    }
}

document.addEventListener("DOMContentLoaded", function () {
    loadAuditLogs();
});



async function loadUsers() {
    const tbody = document.getElementById("usersTableBody");

    if (!tbody) return;

    try {
        const response = await fetch("Backend/UsersHandler.ashx", {
            credentials: "same-origin"
        });

        if (response.status === 401) {
            window.location.href = "login.html";
            return;
        }

        if (response.status === 403) {
            tbody.innerHTML =
                '<tr><td colspan="6">เฉพาะ Admin เท่านั้นที่ดูรายชื่อผู้ใช้ได้</td></tr>';
            return;
        }

        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error("โหลดผู้ใช้ไม่สำเร็จ");
        }

        tbody.replaceChildren();

        if (result.data.length === 0) {
            tbody.innerHTML =
                '<tr><td colspan="6">ยังไม่มีผู้ใช้</td></tr>';
            return;
        }

        result.data.forEach(function (user) {
            const tr = document.createElement("tr");

            const values = [
                user.username,
                user.fullName,
                user.email || "-",
                user.departmentName || "-",
                user.role,
                user.isActive ? "ใช้งาน" : "ปิดใช้งาน"
            ];

            values.forEach(function (value) {
                const td = document.createElement("td");
                td.textContent = value;
                tr.appendChild(td);
            });

            tbody.appendChild(tr);
        });
    } catch (error) {
        tbody.innerHTML =
            '<tr><td colspan="6">ไม่สามารถโหลดรายชื่อผู้ใช้ได้</td></tr>';
    }
}

document.addEventListener("DOMContentLoaded", function () {
    loadUsers();
});



async function loadDepartmentOptions(selectId) {
    const select = document.getElementById(selectId);

    if (!select) return;

    try {
        const response = await fetch("Backend/DepartmentsHandler.ashx", {
            credentials: "same-origin"
        });

        if (response.status === 401) {
            window.location.href = "login.html";
            return;
        }

        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error("โหลดแผนกไม่สำเร็จ");
        }

        select.replaceChildren();

        const placeholder = document.createElement("option");
        placeholder.value = "";
        placeholder.textContent = "เลือกแผนก";
        select.appendChild(placeholder);

        result.data.forEach(function (department) {
            const option = document.createElement("option");
            option.value = department.departmentId;
            option.textContent = department.departmentName;
            select.appendChild(option);
        });
    } catch (error) {
        select.replaceChildren();

        const option = document.createElement("option");
        option.value = "";
        option.textContent = "ไม่สามารถโหลดแผนกได้";
        select.appendChild(option);
    }
}

document.addEventListener("DOMContentLoaded", function () {
    loadDepartmentOptions("department");
    loadDepartmentOptions("departmentId");
});


async function loadDocuments() {
    const tbody = document.getElementById("documentsTableBody");

    if (!tbody) return;

    tbody.innerHTML =
        '<tr><td colspan="7">กำลังโหลดข้อมูล...</td></tr>';

    try {
        const response = await fetch("Backend/DocumentsListHandler.ashx", {
            method: "GET",
            credentials: "same-origin"
        });

        const result = await response.json();

        if (response.status === 401) {
            window.location.href = "login.html";
            return;
        }

        if (!response.ok || !result.success) {
            throw new Error(result.message || "โหลดข้อมูลไม่สำเร็จ");
        }

        tbody.innerHTML = "";

        if (!result.data || result.data.length === 0) {
            tbody.innerHTML =
                '<tr><td colspan="7">ยังไม่มีเอกสาร</td></tr>';
            return;
        }

        result.data.forEach(function (doc) {
            const tr = document.createElement("tr");

            const values = [
                doc.documentName,
                doc.documentType,
                doc.departmentName,
                doc.uploadedBy,
                doc.uploadedAt,
                "พร้อมใช้งาน"
            ];

            values.forEach(function (value) {
                const td = document.createElement("td");
                td.textContent = value || "-";
                tr.appendChild(td);
            });

            const actionCell = document.createElement("td");
            const downloadButton = document.createElement("button");

            downloadButton.type = "button";
            downloadButton.textContent = "ดาวน์โหลด";

            downloadButton.addEventListener("click", function () {
                window.location.href =
                    "Backend/DownloadHandler.ashx?id=" +
                    encodeURIComponent(doc.documentId);
            });

            actionCell.appendChild(downloadButton);
            tr.appendChild(actionCell);

            tbody.appendChild(tr);
        });
    } catch (error) {
        tbody.innerHTML =
            '<tr><td colspan="7">ไม่สามารถโหลดข้อมูลได้ กรุณาลองใหม่</td></tr>';
    }
}

document.addEventListener("DOMContentLoaded", () => {


    // =====================================
    // Dashboard: โหลดข้อมูลจาก SQL Server
    // =====================================

    const documentCount = document.getElementById("dashDocumentCount");
    const monthlyDocuments = document.getElementById("dashMonthlyDocuments");
    const activeUsers = document.getElementById("dashActiveUsers");
    const todayActivities = document.getElementById("dashTodayActivities");

    const documentsBody = document.getElementById("recentDocumentsBody");
    const activitiesBody = document.getElementById("recentActivitiesBody");
    const dashboardMessage = document.getElementById("dashboardMessage");

    async function loadDashboard() {
        // ทำงานเฉพาะหน้า Dashboard
        if (!documentCount || !documentsBody || !activitiesBody) {
            return;
        }

        if (dashboardMessage) {
            dashboardMessage.textContent = "";
        }

        documentCount.textContent = "...";
        if (monthlyDocuments) monthlyDocuments.textContent = "...";
        if (activeUsers) activeUsers.textContent = "...";
        if (todayActivities) todayActivities.textContent = "...";

        documentsBody.innerHTML =
            '<tr><td colspan="3">กำลังโหลดข้อมูล...</td></tr>';

        activitiesBody.innerHTML = `
            <div class="activity">
                <div class="activity-dot">…</div>
                <div><strong>กำลังโหลดกิจกรรม</strong><small>กรุณารอสักครู่</small></div>
            </div>`;

        try {
            const response = await fetch("Backend/DashboardHandler.ashx", {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store",
                headers: { "Accept": "application/json" }
            });

            if (response.status === 401) {
                window.location.replace("login.html");
                return;
            }

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(
                    result.message || "ไม่สามารถโหลดข้อมูล Dashboard ได้"
                );
            }

            const summary = result.summary || {};

            // 1. ตัวเลขสรุปทั้ง 4 ช่อง
            documentCount.textContent =
                Number(summary.documentCount ?? 0).toLocaleString("th-TH");

            if (monthlyDocuments) {
                monthlyDocuments.textContent =
                    Number(summary.monthlyDocumentCount ?? 0)
                        .toLocaleString("th-TH");
            }

            if (activeUsers) {
                activeUsers.textContent =
                    Number(summary.activeUserCount ?? 0)
                        .toLocaleString("th-TH");
            }

            if (todayActivities) {
                todayActivities.textContent =
                    Number(summary.todayActivityCount ?? 0)
                        .toLocaleString("th-TH");
            }

            // ป้องกันข้อความจากฐานข้อมูลถูกแปลเป็น HTML
            function safeText(value) {
                return String(value ?? "")
                    .replace(/[&<>"']/g, function (char) {
                        return {
                            "&": "&amp;",
                            "<": "&lt;",
                            ">": "&gt;",
                            '"': "&quot;",
                            "'": "&#39;"
                        }[char];
                    });
            }

            // แสดงวันที่จาก yyyy-MM-ddTHH:mm:ss
            function formatDashboardDate(value) {
                if (!value) return "-";

                const parts = String(value).split(/[T ]/);
                const dateParts = parts[0].split("-");

                if (dateParts.length !== 3) {
                    return safeText(value);
                }

                const dateText =
                    `${dateParts[2]}/${dateParts[1]}/${dateParts[0]}`;

                const timeText = parts[1]
                    ? parts[1].substring(0, 5)
                    : "";

                return safeText(
                    timeText ? `${dateText} ${timeText}` : dateText
                );
            }

            // 2. เอกสารล่าสุด: ชื่อเอกสาร / แผนก / วันที่
            const documents = result.recentDocuments || [];

            if (documents.length === 0) {
                documentsBody.innerHTML =
                    '<tr><td colspan="3">ยังไม่มีเอกสาร</td></tr>';
            } else {
                documentsBody.innerHTML = documents.map(function (doc) {
                    return `
                        <tr>
                            <td>
                                <div class="doc-cell">
                                    <span class="file-icon">📄</span>
                                    <div>
                                        <strong>${safeText(doc.documentName || "-")}</strong>
                                        <small>${safeText(doc.documentType || "")}</small>
                                    </div>
                                </div>
                            </td>
                            <td>${safeText(doc.departmentName || "-")}</td>
                            <td>${formatDashboardDate(doc.uploadedAt)}</td>
                        </tr>`;
                }).join("");
            }

            // 3. กิจกรรมล่าสุด: ใช้ div ให้ตรงกับ HTML และ CSS เดิม
            const activities = result.recentActivities || [];

            const actionNames = {
                Login: "เข้าสู่ระบบ",
                Logout: "ออกจากระบบ",
                Upload: "อัปโหลดเอกสาร",
                Download: "ดาวน์โหลดเอกสาร",
                Delete: "ลบเอกสาร"
            };

            if (activities.length === 0) {
                activitiesBody.innerHTML = `
                    <div class="activity">
                        <div class="activity-dot">–</div>
                        <div><strong>ยังไม่มีกิจกรรม</strong></div>
                    </div>`;
            } else {
                activitiesBody.innerHTML = activities.map(function (item) {
                    const name = item.fullName || item.username || "ผู้ใช้";
                    const action =
                        actionNames[item.action] || item.action || "กิจกรรม";

                    return `
                        <div class="activity">
                            <div class="activity-dot">•</div>
                            <div>
                                <strong>${safeText(name)}</strong>
                                <div>${safeText(action)}</div>
                                <small>${safeText(item.details || "")}</small>
                                <small>${formatDashboardDate(item.createdAt)}</small>
                            </div>
                        </div>`;
                }).join("");
            }

        } catch (error) {
            console.error("Dashboard error:", error);

            if (dashboardMessage) {
                dashboardMessage.textContent =
                    "ไม่สามารถโหลดข้อมูล Dashboard ได้ กรุณาลองใหม่";
            }

            documentsBody.innerHTML =
                '<tr><td colspan="3">ไม่สามารถโหลดเอกสารได้</td></tr>';

            activitiesBody.innerHTML = `
                <div class="activity">
                    <div class="activity-dot">!</div>
                    <div><strong>ไม่สามารถโหลดกิจกรรมได้</strong></div>
                </div>`;
        }
    }

    // เรียกใช้ทันที เพราะโค้ดนี้ทำงานภายใน DOMContentLoaded ตัวหลัก
    loadDashboard();

    loadDocuments();


    document.querySelectorAll("[data-logout]").forEach(function (button) {
        button.addEventListener("click", async function () {
            button.disabled = true;

            try {
                await fetch("Backend/LogoutHandler.ashx", {
                    method: "POST",
                    credentials: "same-origin"
                });
            } finally {
                window.location.replace("login.html");
            }
        });
    });



    const uploadForm = document.getElementById("uploadForm");

    if (uploadForm) {
        uploadForm.addEventListener("submit", async function (event) {
            event.preventDefault();

            const message = document.getElementById("uploadMessage");
            const submitButton =
                uploadForm.querySelector('button[type="submit"]');

            message.textContent = "";
            submitButton.disabled = true;

            try {
                const formData = new FormData(uploadForm);

                const response = await fetch("Backend/UploadHandler.ashx", {
                    method: "POST",
                    credentials: "same-origin",
                    body: formData
                });

                const result = await response.json();

                if (response.status === 401) {
                    window.location.href = "login.html";
                    return;
                }

                if (!response.ok || !result.success) {
                    message.textContent =
                        result.message || "อัปโหลดไม่สำเร็จ";
                    return;
                }

                message.textContent = result.message;
                uploadForm.reset();

            } catch (error) {
                message.textContent =
                    "ไม่สามารถติดต่อเซิร์ฟเวอร์ได้";
            } finally {
                submitButton.disabled = false;
            }
        });
    }

  const search = document.querySelector("[data-table-search]");
  if (search) {
    search.addEventListener("input", () => {
      const q = search.value.toLowerCase().trim();
      document.querySelectorAll("[data-search-row]").forEach(row => {
        row.style.display = row.innerText.toLowerCase().includes(q) ? "" : "none";
      });
    });
  }

  const fileInput = document.querySelector("#documentFile");
  const fileName = document.querySelector("#fileName");
  if (fileInput && fileName) {
    fileInput.addEventListener("change", () => {
      fileName.textContent = fileInput.files.length ? "ไฟล์ที่เลือก: " + fileInput.files[0].name : "ยังไม่ได้เลือกไฟล์";
    });
  }

  document.querySelectorAll("[data-demo-form]").forEach(form => {
    form.addEventListener("submit", event => {
      event.preventDefault();
      alert("นี่เป็นหน้าเว็บตัวอย่าง ข้อมูลยังไม่ได้บันทึกลงฐานข้อมูลจริง");
    });
  });


    const loginForm = document.getElementById("loginForm");

    if (loginForm) {
        loginForm.addEventListener("submit", async function (event) {
            event.preventDefault();

            const username = document.getElementById("username").value.trim();
            const password = document.getElementById("password").value;
            const message = document.getElementById("loginMessage");
            const button = loginForm.querySelector('button[type="submit"]');

            if (!message) {
                console.error("ไม่พบ element: loginMessage");
                return;
            }

            if (!button) {
                console.error("ไม่พบปุ่ม button[type='submit'] ใน loginForm");
                return;
            }
            message.textContent = "";
            button.disabled = true;

            try {
                const body = new URLSearchParams();
                body.append("username", username);
                body.append("password", password);

                const response = await fetch("Backend/LoginHandler.ashx", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded;charset=UTF-8"
                    },
                    body: body.toString()
                });

                const result = await response.json();

                console.log("Login result:", result);
                console.log("HTTP status:", response.status);

                if (!response.ok || !result.success) {
                    message.textContent =
                        result.message || "เข้าสู่ระบบไม่สำเร็จ";
                    return;
                }

                window.location.href = "dashboard.html";
            } catch (error) {
                message.textContent =
                    "ติดต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่";
            } finally {
                button.disabled = false;
            }
        });
    }



  const toggle = document.querySelector("[data-toggle-password]");
  const password = document.querySelector("#password");
  if (toggle && password) {
    toggle.addEventListener("click", () => {
      password.type = password.type === "password" ? "text" : "password";
      toggle.textContent = password.type === "password" ? "แสดง" : "ซ่อน";
    });
  }
});
