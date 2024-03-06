const sign_in_btn = document.querySelector("#sign-in-btn");
const sign_up_btn = document.querySelector("#sign-up-btn");
const container = document.querySelector(".container");

sign_up_btn.addEventListener("click", () => {
  container.classList.add("sign-up-mode");
});

sign_in_btn.addEventListener("click", () => {
  container.classList.remove("sign-up-mode");
});

document.addEventListener("DOMContentLoaded", function () {
  const signInForm = document.querySelector("#sign-in-form");
  const signUpForm = document.querySelector("#sign-up-form");

  signInForm.addEventListener("submit", function (event) {
    event.preventDefault();
    const username = signInForm.querySelector('input[type="text"]').value;
    const password = signInForm.querySelector('input[type="password"]').value;
    console.log("Sign In:", username, password);
  });

  signUpForm.addEventListener("submit", function (event) {
    event.preventDefault();
    const username = signUpForm.querySelector('input[type="text"]').value;
    const email = signUpForm.querySelector('input[type="email"]').value;
    const password = signUpForm.querySelector('input[type="password"]').value;
    console.log("Sign Up:", username, email, password);
  });
});
