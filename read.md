**hardware config** : 
1. the pos printer = xp-365 B
2. ram in the sistem (in the operator pc) = pc 1. = 8 gb , 2. 4 gb .  
3. storage = 256 gb
4. os = windows 11 pro
5. barcode printer = xp-365 B 

**app config** :
1. app name = Baroda Computer and sale's POS app
2. publisher = BCSdevloper 
3. app logo  = al.png
4. app type = productivity
5. shop name = Baroda Computer and Sales
**what we are making** = we are making a shop management app  . that is connected with tyhe firebase . we are usig the firebase as the backend . 
the **twist** is that we aere not using the firebase auth. we are geting our custom auth sistem . 
the items  = username , password , code . thes are the password and the code is encrypted with the argon2id . this will be a c# app . 
so we need to work smartly . we need a couple of pages . the pages are bellow 

**the pages** = 
1. login
2. dashboard
3. product{
    1. all-product
    2. add-product
    3. production
    4. production history
    5. stock-adjust
    6. stock-report
    7. print-barcode
}
4. sale{
    1. pos
    2. add-sale
    3. sale-return
    4. order
    5. all-sale-history
    6. add-sale-history
    7. pos-history
    8. sell-return-history
    9. order-history
    10. due-history
    11. due pay-history
    12. collect-order-return
    13. approve-order-return
}
5. purchase{
    1. purchase
    2. purchase-return
    3. purchase-history
    4. purchase-return-history
}
6. report{
    1. day-report
    3. month-report
    4. profit-and-loss
    5. day-report-history
}
7. people {
    1. all-customer
    2. all-supplier
    3. add-customer
    4. add-supplier
    5. add-delivery-man
    6. all-deliver-man
    7. varify-customer-card
}
8. settings{
    1. settings
    2. account senter
    3. all-employee
    4. add-employee
}
9. mail {
    1. send-mail 
    2. send-mail-history
}

**login** =
auth items = username , password(e) , code(e) . e for the encrypted . collection in the firestore is users . 
after login the user will eb rediect to the dashboard as well . 

**/** = 
thsi si the main wondow . use the windows api to get the theme colors and the titlebar as asame as the windows 11  . in thsi ther will be a topbar , sidebar , footer
1. topbar = 
    1. the shop name 
    2. logout buttion 
    3. pos bution 
    4. task buttiion 
    5. chat buttion
    6. dayreport buttion (this will show in the screen as a pop up not open the dayreport page as well )
    7. a date and time  . format = dd/mm/yyyy | hh:mm:ss 
    8. on sxcreen calculator
2. sidebar = 
1. the links as the pages . 
2. a search bar for the links

3. footer = 
1. allways attach at the down of the page an always show 
2. data in the footer = made with {knowlage-simbol} in bangladesh[https://www.google.com/search?q=bangladehs&sca_esv=813e191b57523132&sxsrf=APpeQnvfcIuP-VC2O4SEWEPxbcIv66xrKA%3A1784464639363&ei=_8RcaqveFc3tseMPjuSLyAw&biw=1920&bih=945&ved=0ahUKEwjr2cXn4N6VAxXNdmwGHQ7yAskQ4dUDCBA&uact=5&oq=bangladehs&gs_lp=Egxnd3Mtd2l6LXNlcnAiCmJhbmdsYWRlaHMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyChAAGEcY1gQYsAMyDRAAGIAEGIoFGEMYsAMyDRAAGIAEGIoFGEMYsAMyDRAAGIAEGIoFGEMYsAMyDRAAGIAEGIoFGEMYsAMyDhAAGOQCGNYEGLAD2AEBMg4QABjkAhjWBBiwA9gBATIOEAAY5AIY1gQYsAPYAQEyExAuGIAEGIoFGEMYyAMYsAPYAQEyExAuGIAEGIoFGEMYyAMYsAPYAQEyExAuGIAEGIoFGEMYyAMYsAPYAQFIsApQAFgAcAF4AZABAJgBAKABAKoBALgBA8gBAJgCAaACC5gDAOIDBRIBMSBAiAYBkAYSugYGCAEQARgJkgcBMaAHALIHALgHAMIHAzMtMcgHCYAIAQ&sclient=gws-wiz-serp] | vershion: {current vershion} | developed by :Indranil saha[https://indranil-saha.web.app] and BCSdevloper[https://bcsdevloper.web.app] | powred by google cloud[https://cloud.google.com/] and Firebase[https://firebase.google.com/]