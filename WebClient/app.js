import { OpenAPI, MailService } from "./generated-js/index.js";

const mailForm = document.getElementById("mailForm");
const resultElement = document.getElementById("result");
const statusElement = document.getElementById("status");
const fillExampleButton = document.getElementById("fillExample");
let clickButtonTimes = 0;

mailForm.addEventListener("submit", async function (event) {
  event.preventDefault();

  const apiUrl = document.getElementById("apiUrl").value.trim();
  const token = document.getElementById("token").value.trim();
  const to = document.getElementById("to").value.trim();
  const subject = document.getElementById("subject").value.trim();
  const body = document.getElementById("body").value.trim();

  if (!apiUrl || !token || !to || !subject || !body) {
    showResult("Uzupełnij wszystkie pola formularza.", "Błąd walidacji");
    return;
  }

  OpenAPI.BASE = apiUrl;
  OpenAPI.TOKEN = token;

  const requestBody = {
    to: to,
    subject: subject,
    body: body,
  };

  try {
    setLoadingState(true);

    const response = await MailService.postMailSend(requestBody);

    showResult(response, "Wysłano poprawnie");
  } catch (error) {
    showResult(
      {
        message: "Wystąpił błąd podczas wysyłania wiadomości.",
        error: formatError(error),
      },
      "Błąd API",
    );
  } finally {
    setLoadingState(false);
  }
});

fillExampleButton.addEventListener("click", function () {
  if (clickButtonTimes % 2 == 0) {
    document.getElementById("to").value =
      "krystian.haberka@microsoft.wsei.edu.pl";
    document.getElementById("subject").value = "Czy Brevo działa?";
    document.getElementById("body").value = "To jest test od Haberka.";
  } else {
    document.getElementById("to").value =
      "magdalena.kapusta@microsoft.wsei.edu.pl";
    document.getElementById("subject").value = "Czy Brevo działa?";
    document.getElementById("body").value = "To jest test od Kapusta.";
  }
  clickButtonTimes++;
});

function showResult(data, statusText) {
  statusElement.textContent = statusText;

  if (typeof data === "string") {
    resultElement.textContent = data;
    return;
  }

  resultElement.textContent = JSON.stringify(data, null, 2);
}

function setLoadingState(isLoading) {
  const submitButton = mailForm.querySelector("button[type='submit']");

  if (isLoading) {
    submitButton.disabled = true;
    submitButton.textContent = "Wysyłanie...";
    statusElement.textContent = "Wysyłanie requestu...";
  } else {
    submitButton.disabled = false;
    submitButton.textContent = "Wyślij wiadomość";
  }
}

function formatError(error) {
  if (error && typeof error === "object") {
    return {
      name: error.name,
      message: error.message,
      status: error.status,
      statusText: error.statusText,
      body: error.body,
    };
  }

  return String(error);
}
