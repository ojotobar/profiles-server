using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Extensions
{
    public static class SeederExtensions
    {
        public static List<Faqs> GetSystemFaqs()
        {
            return [
                new Faqs
                {
                    Title = "What is Pro-files?",
                    Content = "Pro-files is a platform that allows professionals to showcase their skills, work experience, and projects effortlessly."
                },
                new Faqs
                {
                    Title = "Who can use Pro-files?",
                    Content = "Profiles is designed for professionals across all industries, including freelancers, job seekers, and business owners."
                },
                new Faqs
                {
                    Title = "How do I sign up for Pro-files?",
                    Content = "You can sign up by visiting our homepage and clicking the \"Sign Up\" button. Follow the guided steps to complete your profile."
                },
                new Faqs
                {
                    Title = "Is Pro-files free to use?",
                    Content = "Profiles offers both free and premium plans. The free plan provides basic features, while the premium plan includes additional customization and advanced tools."
                },
                new Faqs
                {
                    Title = "How can I edit my profile information?",
                    Content = "Log into your account, go to the \"Profile\" section, and click \"Edit\" to update your details."
                },
                new Faqs
                {
                    Title = "What type of content can I add to my profile?",
                    Content = "You can add work experience, projects, certifications, skills, qualifications, and personal achievements."
                },
                new Faqs
                {
                    Title = "Can I upload documents or images to my profile?",
                    Content = "Yes, you can upload resumes, certificates, project images, and other supporting documents."
                },
                new Faqs
                {
                    Title = "How can I showcase my projects?",
                    Content = "Under the \"Projects\" section of your profile, you can add project descriptions, links, and images to highlight your work."
                },
                new Faqs
                {
                    Title = "Can I link my social media or portfolio website?",
                    Content = "Yes! You can add links to LinkedIn, GitHub, or any other external websites."
                },
                new Faqs
                {
                    Title = "Is there a way to get recommendations or endorsements?",
                    Content = "Yes, your connections can endorse your skills or write testimonials on your profile."
                },
                new Faqs
                {
                    Title = "How can I improve the visibility of my profile?",
                    Content = "Ensure your profile is complete, add relevant keywords, and share your profile link on social media and job boards."
                },
                new Faqs
                {
                    Title = "What payment methods are accepted for the premium plan?",
                    Content = "We accept credit/debit cards, PayPal, and bank transfers."
                },
                new Faqs
                {
                    Title = "Can I cancel my subscription at any time?",
                    Content = "Yes, you can cancel your subscription anytime through your account settings. Your premium features will remain active until the end of your billing cycle."
                },
                new Faqs
                {
                    Title = "Do you offer refunds?",
                    Content = "We offer a 7-day refund policy for first-time subscribers."
                },
                new Faqs
                {
                    Title = "I forgot my password. How can I reset it?",
                    Content = "Click \"Forgot Password\" on the login page and follow the instructions to reset your password."
                },
                new Faqs
                {
                    Title = "How do I contact customer support?",
                    Content = "You can reach us through our contact page, email, or live chat support."
                },
                new Faqs
                {
                    Title = "Is my data secure on Pro-files?",
                    Content = "Yes, we use industry-standard encryption and security measures to protect your data."
                },
                new Faqs
                {
                    Title = "Can I delete my account?",
                    Content = "Yes, you can request account deletion from your settings, but this action is permanent."
                },
                new Faqs
                {
                    Title = "Do you have a mobile app?",
                    Content = "Currently, Profiles is available on the web, but a mobile app is in development."
                },
                new Faqs
                {
                    Title = "After completing my details on Pro-files, what next?",
                    Content = "You'll be able to generate the API key necessary to deploy our Front-end Docker image. This would serve as your porfolio link that you can share to showcase your skills and achievements."
                },
                new Faqs
                {
                    Title = "What if I want to develop my portfolio front-end?",
                    Content = "You can integrate your personal portfolio with our backend service if you so wish. You can find the link to the API section on our documentation page.."
                }
            ];
        }
    }
}
